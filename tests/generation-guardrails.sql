-- Run against the linked PhotoBooth project. Every fixture and counter change rolls back.
-- No HTTP/provider call is possible in this test.
begin;
set local lock_timeout = '5s';
set local statement_timeout = '20s';
lock table public.booth_budget in exclusive mode;
do $$
declare sid uuid := gen_random_uuid(); other_id uuid := gen_random_uuid(); owner text := repeat('f',64); before_count integer;
begin
  if has_function_privilege('anon','public.booth_claim_job(uuid,integer,text)','execute') or
     has_function_privilege('authenticated','public.booth_claim_job(uuid,integer,text)','execute') then
    raise exception 'Public role can claim a paid generation';
  end if;
  if has_table_privilege('anon','public.booth_sessions','select') or
     has_table_privilege('authenticated','public.booth_budget','update') then raise exception 'Public data privileges'; end if;
  if exists(select from pg_proc where oid='public.booth_claim_job(uuid,integer,text)'::regprocedure and prosecdef) then
    raise exception 'Claim should run as invoker';
  end if;
  if exists(select from pg_class where oid in ('public.booth_sessions'::regclass,'public.booth_assets'::regclass,'public.booth_jobs'::regclass,'public.booth_budget'::regclass) and not relrowsecurity) then
    raise exception 'Missing RLS';
  end if;
  if exists(select from storage.buckets where id='booth-private' and public) then raise exception 'Public photo bucket'; end if;
  select started into before_count from public.booth_budget where id='event';
  update public.booth_budget set generation_enabled=true, max_images=2400, started=0,max_per_minute=60,max_in_flight=16;
  -- Existing jobs must not affect the isolated assertions, and are restored by rollback.
  update public.booth_jobs set state='ready',claimed_at=null;
  insert into public.booth_sessions(id,owner_hash,photo_count,mode,style) values(sid,owner,4,'surprise','colors_party'),(other_id,owner,4,'surprise','colors_party');
  if not public.booth_claim_job(sid,0,owner) then raise exception 'First claim rejected'; end if;
  if public.booth_claim_job(sid,0,owner) then raise exception 'Duplicate charged'; end if;
  if (select started from public.booth_budget where id='event') <> 1 then raise exception 'Duplicate changed budget'; end if;
  update public.booth_budget set max_in_flight=1;
  begin perform public.booth_claim_job(other_id,0,owner); raise exception 'Busy accepted';
  exception when raise_exception then if sqlerrm <> 'generation_busy' then raise; end if; end;
  update public.booth_budget set max_in_flight=16,max_per_minute=1;
  begin perform public.booth_claim_job(other_id,0,owner); raise exception 'Rate limit accepted';
  exception when raise_exception then if sqlerrm <> 'generation_rate_limited' then raise; end if; end;
  update public.booth_budget set max_per_minute=60,max_images=1;
  begin perform public.booth_claim_job(sid,1,owner); raise exception 'Quota accepted';
  exception when raise_exception then if sqlerrm <> 'event_quota_reached' then raise; end if; end;
  update public.booth_budget set max_images=2400,generation_enabled=false;
  begin perform public.booth_claim_job(sid,1,owner); raise exception 'Disabled accepted';
  exception when raise_exception then if sqlerrm <> 'generation_disabled' then raise; end if; end;
  if (select started from public.booth_budget where id='event') <> 1 then raise exception 'Rejected claim spent budget'; end if;
  update public.booth_budget set generation_enabled=true;
  update public.booth_jobs set state='uncertain' where session_id=sid;
  if public.booth_claim_job(sid,0,owner) then raise exception 'Uncertain claim retried'; end if;
  begin perform public.booth_claim_job(sid,1,repeat('a',64)); raise exception 'Wrong owner accepted';
  exception when raise_exception then if sqlerrm <> 'invalid_session' then raise; end if; end;
  update public.booth_sessions set published_at=now() where id=sid;
  begin perform public.booth_claim_job(sid,1,owner); raise exception 'Published session generated';
  exception when raise_exception then if sqlerrm <> 'already_published' then raise; end if; end;
  raise notice 'PASS: idempotency, quota, rate, concurrency cap, kill switch, ownership, publication, uncertain state, grants, RLS, private bucket. Previous started=%; all changes roll back.', before_count;
end $$;
rollback;
select id, started, max_images, max_per_minute, max_in_flight, generation_enabled from public.booth_budget;
