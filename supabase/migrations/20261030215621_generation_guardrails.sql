alter table public.booth_budget
  add column generation_enabled boolean not null default true,
  add column max_per_minute integer not null default 8 check (max_per_minute between 1 and 60),
  add column max_in_flight integer not null default 4 check (max_in_flight between 1 and 16);
alter table public.booth_jobs add column claimed_at timestamptz;
create index booth_jobs_claimed on public.booth_jobs(claimed_at) where claimed_at is not null;
create index booth_jobs_active on public.booth_jobs(state) where state in ('starting','running','uncertain');

-- All checks and the budget increment share a database lock, across Edge instances.
-- Ambiguous jobs keep their slot until an operator checks the provider outcome.
create or replace function public.booth_claim_job(p_session uuid, p_index integer, p_owner text)
returns boolean language plpgsql security invoker set search_path = '' as $$
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
  if s.published_at is not null then raise exception 'already_published'; end if;
  select * into b from public.booth_budget where id='event' for update;
  if not found then raise exception 'generation_disabled'; end if;
  if not b.generation_enabled then raise exception 'generation_disabled'; end if;
  if b.started >= b.max_images then raise exception 'event_quota_reached'; end if;
  if (select count(*) from public.booth_jobs where claimed_at > clock_timestamp() - interval '1 minute') >= b.max_per_minute then
    raise exception 'generation_rate_limited';
  end if;
  if (select count(*) from public.booth_jobs where state in ('starting','running','uncertain')) >= b.max_in_flight then
    raise exception 'generation_busy';
  end if;
  update public.booth_budget set started=started+1 where id='event';
  update public.booth_jobs set state='starting', claimed_at=clock_timestamp(), updated_at=now()
    where session_id=p_session and shot_index=p_index;
  return true;
end $$;
revoke all on function public.booth_claim_job(uuid,integer,text) from public, anon, authenticated;
grant execute on function public.booth_claim_job(uuid,integer,text) to service_role;
