-- Separate, read-only projection capability. Never reuse the booth or guest key.
create table public.booth_viewers (
  owner_hash text primary key check (owner_hash ~ '^[a-f0-9]{64}$'),
  token_hash text not null unique check (token_hash ~ '^[a-f0-9]{64}$'),
  enabled boolean not null default false,
  include_tests boolean not null default false,
  updated_at timestamptz not null default now()
);
create table public.booth_projections (
  session_id uuid primary key references public.booth_sessions(id) on delete cascade,
  source text not null check (source in ('camera','fixture')),
  released_at timestamptz not null default now()
);
create index booth_projections_release on public.booth_projections(released_at desc);
alter table public.booth_viewers enable row level security;
alter table public.booth_projections enable row level security;
revoke all on public.booth_viewers, public.booth_projections from public, anon, authenticated;
grant select, insert, update on public.booth_viewers to service_role;
grant select, insert on public.booth_projections to service_role;

-- Only completed, released sessions. A surprise fallback NEVER exposes originals.
create function public.booth_viewer_feed(p_owner text) returns jsonb
language sql stable security invoker set search_path = '' as $$
 select coalesce(jsonb_agg(to_jsonb(ready) order by ready.released_at,ready.id),'[]'::jsonb)
 from (
   select s.id,s.photo_count,s.mode,s.style,p.released_at,
     jsonb_agg(jsonb_build_object('name',a.name,'key',a.object_key) order by a.name) as photos
   from public.booth_projections p
   join public.booth_sessions s on s.id=p.session_id
   join public.booth_viewers v on v.owner_hash=s.owner_hash and v.enabled
   join public.booth_assets a on a.session_id=s.id and a.uploaded and a.name ~ '^photo-[1-4]\.jpg$'
   where s.owner_hash=p_owner and s.expires_at>now() and s.published_at is not null
     and (v.include_tests or p.source='camera')
     and not (s.mode='surprise' and s.used_original_fallback)
   group by s.id,p.released_at
   having count(*)=s.photo_count
   order by p.released_at desc,s.id desc limit 200
 ) ready;
$$;
revoke all on function public.booth_viewer_feed(text) from public, anon, authenticated;
grant execute on function public.booth_viewer_feed(text) to service_role;
