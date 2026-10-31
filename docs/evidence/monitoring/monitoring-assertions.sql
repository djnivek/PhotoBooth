-- Run inside a transaction; synthetic measurements and migration DDL are rolled back.
do $$
declare a uuid:=gen_random_uuid(); b uuid:=gen_random_uuid(); viewer uuid:=gen_random_uuid(); base jsonb; sim jsonb; guest jsonb; report jsonb; inc jsonb; initial bigint; t jsonb;
begin
  initial:=(public.booth_monitoring_report(7,false)->'totals'->>'generated')::bigint;
  base:=jsonb_build_object('id',gen_random_uuid(),'session_id',a,'origin','booth','kind','session_started','occurred_at',now(),'mode','surprise','photo_count',4,'requested_style','halloween','source','camera','shot_index',-1);
  sim:=base||jsonb_build_object('id',gen_random_uuid(),'session_id',b,'source','fixture');
  perform public.booth_metrics_ingest(jsonb_build_array(base,
    base||jsonb_build_object('id',gen_random_uuid(),'kind','capture_saved','shot_index',0),
    base||jsonb_build_object('id',gen_random_uuid(),'kind','capture_saved','shot_index',1),
    base||jsonb_build_object('id',gen_random_uuid(),'kind','upload','asset','input-1.jpg','duration_ms',1000,'bytes',100,'outcome','success'),
    base||jsonb_build_object('id',gen_random_uuid(),'kind','upload','asset','input-2.jpg','duration_ms',3000,'bytes',300,'outcome','success'),
    base||jsonb_build_object('id',gen_random_uuid(),'kind','upload','asset','input-3.jpg','duration_ms',9000,'bytes',50,'outcome','failed'),
    base||jsonb_build_object('id',gen_random_uuid(),'origin','provider','kind','generation','shot_index',0,'duration_ms',2000,'outcome','success'),
    sim,sim||jsonb_build_object('id',gen_random_uuid(),'kind','capture_saved','shot_index',0),
    sim||jsonb_build_object('id',gen_random_uuid(),'origin','provider','kind','generation','shot_index',0,'duration_ms',500,'outcome','success')));
  -- Replayed event IDs and lifecycle keys never inflate totals.
  perform public.booth_metrics_ingest(jsonb_build_array(base,base||jsonb_build_object('id',gen_random_uuid())));
  guest:=base||jsonb_build_object('id',gen_random_uuid(),'origin','guest','kind','gallery_view','view_id',viewer,'source','unknown');
  perform public.booth_metrics_ingest(jsonb_build_array(guest,guest||jsonb_build_object('id',gen_random_uuid()),
    guest||jsonb_build_object('id',gen_random_uuid(),'kind','image_view','asset','photo-1.jpg'),
    guest||jsonb_build_object('id',gen_random_uuid(),'kind','image_view','asset','photo-1.jpg'),
    guest||jsonb_build_object('id',gen_random_uuid(),'kind','download_requested','asset','photo-1.jpg'),
    guest||jsonb_build_object('id',gen_random_uuid(),'kind','download_requested','asset','photo-1.jpg')));
  report:=public.booth_monitoring_report(7,false);inc:=public.booth_monitoring_report(7,true);
  if (report->'totals'->>'captured')::integer<>2 or (inc->'totals'->>'captured')::integer<>3 then raise exception 'Capture/fixture counts wrong'; end if;
  if (report->'totals'->>'sessions')::integer<>1 or (report->'totals'->>'generated')::bigint<>initial+1 then raise exception 'Session/generation counts wrong'; end if;
  if (report->'totals'->>'gallery_views')::integer<>1 or (report->'totals'->>'image_views')::integer<>1 or (report->'totals'->>'download_requests')::integer<>2 then raise exception 'Guest dedup/click counts wrong'; end if;
  select value into t from jsonb_array_elements(report->'timings') where value->>'kind'='upload';
  if (t->>'avg_ms')::numeric<>2000 or (t->>'timed_samples')::integer<>2 or (t->>'unsuccessful')::integer<>1 or (t->>'p95_ms')::numeric<>2900 then raise exception 'Timing average/percentile wrong: %',t; end if;
  if has_table_privilege('anon','public.booth_metrics','SELECT') or has_table_privilege('authenticated','public.booth_metrics','INSERT') or has_function_privilege('anon','public.booth_monitoring_report(integer,boolean)','EXECUTE') or has_function_privilege('authenticated','public.booth_metrics_ingest(jsonb)','EXECUTE') then raise exception 'Private metrics privileges wrong'; end if;
  if not (select relrowsecurity from pg_class where oid='public.booth_metrics'::regclass) then raise exception 'Metrics RLS disabled'; end if;
end $$;
