create table public.booth_sessions (
  id uuid primary key,
  owner_hash text not null check (owner_hash ~ '^[a-f0-9]{64}$'),
  photo_count smallint not null check (photo_count in (1,4)),
  mode text not null check (mode in ('original','surprise')),
  style text not null,
  share_hash text unique check (share_hash ~ '^[a-f0-9]{64}$'),
  published_at timestamptz,
  used_original_fallback boolean not null default false,
  created_at timestamptz not null default now(),
  expires_at timestamptz not null default now() + interval '7 days'
);
create index booth_sessions_expiry on public.booth_sessions(expires_at);
create table public.booth_jobs (
  session_id uuid not null references public.booth_sessions(id) on delete cascade,
  shot_index smallint not null check (shot_index between 0 and 3),
  state text not null default 'queued' check (state in ('queued','starting','running','ready','failed','uncertain')),
  response_id text,
  object_key text,
  usage jsonb,
  updated_at timestamptz not null default now(),
  primary key (session_id,shot_index)
);
create table public.booth_assets (
  session_id uuid not null references public.booth_sessions(id) on delete cascade,
  name text not null check (name ~ '^(photo-[1-4]\.jpg|sheet\.jpg|archive\.zip)$'),
  object_key text not null unique,
  sha256 text not null check (sha256 ~ '^[a-f0-9]{64}$'),
  bytes bigint not null check (bytes between 1 and 16777216),
  uploaded boolean not null default false,
  primary key (session_id,name)
);
create table public.booth_budget (
  id text primary key check (id = 'event'),
  started integer not null default 0 check (started >= 0),
  max_images integer not null default 2400 check (max_images between 0 and 2400)
);
insert into public.booth_budget(id) values ('event');

alter table public.booth_sessions enable row level security;
alter table public.booth_jobs enable row level security;
alter table public.booth_assets enable row level security;
alter table public.booth_budget enable row level security;
revoke all on public.booth_sessions, public.booth_jobs, public.booth_assets, public.booth_budget from anon, authenticated;
grant all on public.booth_sessions, public.booth_jobs, public.booth_assets, public.booth_budget to service_role;

-- Atomic claim. Ambiguous provider calls are NEVER made eligible for automatic retry.
create function public.booth_claim_job(p_session uuid, p_index integer, p_owner text)
returns boolean language plpgsql security definer set search_path = '' as $$
declare s public.booth_sessions; j public.booth_jobs; b public.booth_budget;
begin
  select * into s from public.booth_sessions where id = p_session for update;
  if not found or s.owner_hash <> p_owner or s.mode <> 'surprise' or
    s.expires_at <= now() or p_index < 0 or p_index >= s.photo_count then
    raise exception 'invalid_session';
  end if;
  insert into public.booth_jobs(session_id, shot_index) values(p_session,p_index) on conflict do nothing;
  select * into j from public.booth_jobs where session_id=p_session and shot_index=p_index for update;
  if j.state <> 'queued' then return false; end if;
  select * into b from public.booth_budget where id='event' for update;
  if b.started >= b.max_images then raise exception 'event_quota_reached'; end if;
  update public.booth_budget set started=started+1 where id='event';
  update public.booth_jobs set state='starting', updated_at=now() where session_id=p_session and shot_index=p_index;
  return true;
end $$;
revoke all on function public.booth_claim_job(uuid,integer,text) from public, anon, authenticated;
grant execute on function public.booth_claim_job(uuid,integer,text) to service_role;

insert into storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
values('booth-private','booth-private',false,16777216,array['image/jpeg','application/zip'])
on conflict (id) do nothing;
-- No anon/authenticated storage policies: only the server role and signed URLs.
