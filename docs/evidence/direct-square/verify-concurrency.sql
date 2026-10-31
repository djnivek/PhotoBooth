create or replace function pg_temp.verify_photobooth_concurrency()
returns jsonb language plpgsql as $$
declare
  original public.booth_budget;
  after_test public.booth_budget;
  ids uuid[] := array[]::uuid[];
  sid uuid;
  owner text := repeat('0',64);
  checks text[] := array[]::text[];
  i integer;
begin
  select * into strict original from public.booth_budget where id='event' for update;
  if original.max_in_flight <> 8 or original.max_per_minute <> 16 or original.max_images <> 2400 or not original.generation_enabled then
    raise exception 'Unexpected event configuration';
  end if;
  if exists(select 1 from public.booth_jobs where state in ('starting','running','uncertain') or claimed_at > clock_timestamp()-interval '1 minute') then
    raise exception 'Wait until real jobs are idle before this isolated guardrail test';
  end if;
  if original.started+16 > original.max_images then raise exception 'Insufficient remaining event budget for rollback test'; end if;
  begin
    for i in 1..5 loop
      sid := gen_random_uuid(); ids := array_append(ids,sid);
      insert into public.booth_sessions(id,owner_hash,photo_count,mode,style)
      values(sid,owner,4,'surprise','halloween');
    end loop;
    for i in 0..7 loop
      if not public.booth_claim_job(ids[i/4+1],i%4,owner) then raise exception 'Claim not accepted'; end if;
    end loop;
    if (select count(*) from public.booth_jobs where session_id=any(ids) and state='starting') <> 8 then raise exception 'Eight claims not active'; end if;
    checks:=array_append(checks,'Eight distinct claims accepted across two four-pose sessions');
    if public.booth_claim_job(ids[1],0,owner) then raise exception 'Duplicate accepted'; end if;
    checks:=array_append(checks,'Duplicate claim does not consume another image');
    begin
      perform public.booth_claim_job(ids[3],0,owner);
      raise exception 'Ninth active job incorrectly accepted';
    exception when raise_exception then
      if sqlerrm <> 'generation_busy' then raise; end if;
    end;
    checks:=array_append(checks,'Ninth unresolved job rejected by the concurrency limit');
    update public.booth_jobs set state='failed' where session_id=any(ids);
    for i in 8..15 loop
      if not public.booth_claim_job(ids[i/4+1],i%4,owner) then raise exception 'Subsequent claim not accepted'; end if;
      update public.booth_jobs set state='failed' where session_id=ids[i/4+1] and shot_index=i%4;
    end loop;
    begin
      perform public.booth_claim_job(ids[5],0,owner);
      raise exception 'Seventeenth minute claim incorrectly accepted';
    exception when raise_exception then
      if sqlerrm <> 'generation_rate_limited' then raise; end if;
    end;
    checks:=array_append(checks,'Sixteen starts per minute enforced even after jobs finish');
    update public.booth_budget set generation_enabled=false where id='event';
    begin
      perform public.booth_claim_job(ids[5],0,owner);
      raise exception 'Kill switch not enforced';
    exception when raise_exception then
      if sqlerrm <> 'generation_disabled' then raise; end if;
    end;
    checks:=array_append(checks,'Operator kill switch rejects new claims');
    update public.booth_budget set generation_enabled=true,started=max_images where id='event';
    begin
      perform public.booth_claim_job(ids[5],0,owner);
      raise exception 'Event budget not enforced';
    exception when raise_exception then
      if sqlerrm <> 'event_quota_reached' then raise; end if;
    end;
    checks:=array_append(checks,'Total event budget remains enforced');
    raise exception using errcode='Z0001',message='Rollback synthetic fixtures and counters';
  exception when sqlstate 'Z0001' then null;
  end;
  select * into strict after_test from public.booth_budget where id='event';
  if after_test is distinct from original or exists(select 1 from public.booth_sessions where id=any(ids)) then
    raise exception 'Rollback did not preserve original state';
  end if;
  return jsonb_build_object('success',true,'project','frkckrvsdsiwivdfyjau','checks',checks,
    'transaction_rolled_back',true,'generation_calls',0,'started_before',original.started,
    'started_after',after_test.started,'max_in_flight',after_test.max_in_flight,
    'max_per_minute',after_test.max_per_minute,'max_images',after_test.max_images);
end $$;
select pg_temp.verify_photobooth_concurrency() as verification;
