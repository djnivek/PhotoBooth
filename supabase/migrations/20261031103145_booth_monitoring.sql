-- Long-lived business measurements, separate from Supabase diagnostic log retention.
create table public.booth_metrics (
  id uuid primary key,
  session_id uuid not null,
  origin text not null check (origin in ('booth','provider','guest')),
  kind text not null check (kind in ('session_started','capture_saved','session_ready','session_failed','session_cancelled','original_fallback','upload','download','generation','gallery_view','image_view','download_requested')),
  occurred_at timestamptz not null,
  received_at timestamptz not null default now(),
  mode text not null check (mode in ('original','surprise')),
  photo_count smallint not null check (photo_count in (1,4)),
  requested_style text not null check (requested_style ~ '^[a-z][a-z0-9_-]{0,47}$'),
  style text check (style ~ '^[a-z][a-z0-9_-]{0,47}$'),
  source text not null check (source in ('camera','fixture','unknown')),
  shot_index smallint not null default -1 check (shot_index between -1 and 3),
  duration_ms bigint check (duration_ms between 0 and 900000),
  bytes bigint check (bytes between 0 and 52428800),
  asset text check (asset ~ '^(input-[1-4]\.jpg|original-[1-4]\.jpg|photo-[1-4]\.jpg|sheet\.jpg|archive\.zip)$'),
  outcome text check (outcome in ('success','failed','uncertain')),
  view_id uuid
);
create index booth_metrics_time on public.booth_metrics(occurred_at);
create index booth_metrics_session on public.booth_metrics(session_id,origin);
create index booth_metrics_sources on public.booth_metrics(session_id,source) where kind='session_started';
create unique index booth_metrics_lifecycle on public.booth_metrics(session_id,kind,shot_index)
where origin='booth' and kind not in ('upload','download');
create unique index booth_metrics_generation on public.booth_metrics(session_id,shot_index) where origin='provider' and kind='generation';
create unique index booth_metrics_guest_view on public.booth_metrics(session_id,view_id,kind,coalesce(asset,''))
where origin='guest' and kind in ('gallery_view','image_view');
alter table public.booth_metrics enable row level security;
revoke all on public.booth_metrics from public,anon,authenticated;
grant select,insert on public.booth_metrics to service_role;

create function public.booth_metrics_ingest(p_events jsonb)
returns integer language plpgsql security invoker set search_path='' as $$
declare e jsonb; item public.booth_metrics;
begin
  if jsonb_typeof(p_events) <> 'array' or jsonb_array_length(p_events) not between 1 and 10 then raise exception 'invalid_metrics'; end if;
  for e in select value from jsonb_array_elements(p_events) loop
    item:=jsonb_populate_record(null::public.booth_metrics,e);
    perform pg_advisory_xact_lock(hashtextextended(item.session_id::text,0));
    if exists(select 1 from public.booth_metrics where id=item.id) then continue; end if;
    if item.origin='guest' and (select count(*) from public.booth_metrics where session_id=item.session_id and origin='guest')>=5000 then raise exception 'metrics_rate_limited'; end if;
    if item.origin='booth' and (select count(*) from public.booth_metrics where session_id=item.session_id and origin='booth')>=1000 then raise exception 'metrics_rate_limited'; end if;
    insert into public.booth_metrics(id,session_id,origin,kind,occurred_at,mode,photo_count,requested_style,style,source,shot_index,duration_ms,bytes,asset,outcome,view_id)
    values(item.id,item.session_id,item.origin,item.kind,item.occurred_at,item.mode,item.photo_count,item.requested_style,item.style,item.source,item.shot_index,item.duration_ms,item.bytes,item.asset,item.outcome,item.view_id)
    on conflict do nothing;
  end loop;
  return jsonb_array_length(p_events);
end $$;
revoke all on function public.booth_metrics_ingest(jsonb) from public,anon,authenticated;
grant execute on function public.booth_metrics_ingest(jsonb) to service_role;

-- Preserve known historical successful generations, without inventing latency/captures.
insert into public.booth_metrics(id,session_id,origin,kind,occurred_at,mode,photo_count,requested_style,style,source,shot_index,outcome)
select gen_random_uuid(),j.session_id,'provider','generation',j.updated_at,s.mode,s.photo_count,coalesce(s.requested_style,'chef'),s.style,'unknown',j.shot_index,'success'
from public.booth_jobs j join public.booth_sessions s on s.id=j.session_id where j.state='ready'
on conflict do nothing;

create function public.booth_monitoring_report(p_days integer default 7,p_include_fixtures boolean default false)
returns jsonb language sql stable security invoker set search_path='' as $$
with session_sources as (
  select session_id,max(source) filter(where source<>'unknown') as source from public.booth_metrics where kind='session_started' group by session_id
), events as (
  select e.*,coalesce(s.source,e.source) as effective_source from public.booth_metrics e left join session_sources s using(session_id)
  where occurred_at >= now()-make_interval(days=>greatest(1,least(p_days,366)))
    and (p_include_fixtures or coalesce(s.source,e.source)<>'fixture')
), timings as (
  select kind,count(*) filter(where outcome='success') as successful,
    count(duration_ms) filter(where outcome='success') as timed_samples,
    count(*) filter(where outcome<>'success') as unsuccessful,
    round(avg(duration_ms) filter(where outcome='success'),1) as avg_ms,
    percentile_cont(0.5) within group(order by duration_ms) filter(where outcome='success') as p50_ms,
    percentile_cont(0.95) within group(order by duration_ms) filter(where outcome='success') as p95_ms,
    coalesce(sum(bytes) filter(where outcome='success'),0) as bytes
  from events where kind in ('upload','download','generation') group by kind
), modes as (
  select mode,count(*) as sessions,sum(photo_count) as requested_photos from events where kind='session_started' group by mode
), themes as (
  select requested_style,count(*) as sessions from events where kind='session_started' and mode='surprise' group by requested_style
), resolved as (
  select style,count(*) as generated_images from events where kind='generation' and outcome='success' group by style
), downloads as (
  select asset,count(*) as requests from events where kind='download_requested' group by asset
), per_session as (
  select session_id,count(*) filter(where kind='capture_saved') as captures from events group by session_id
), hourly as (
  select date_trunc('hour',occurred_at) as hour,
    count(*) filter(where kind='session_started') as sessions,
    count(*) filter(where kind='capture_saved') as captured,
    count(*) filter(where kind='generation' and outcome='success') as generated
  from events group by 1 order by 1
)
select jsonb_build_object(
  'generated_at',now(),'collection_started_at',(select min(received_at) from public.booth_metrics),'days',greatest(1,least(p_days,366)),'include_fixtures',p_include_fixtures,
  'definitions',jsonb_build_object('captures','Saved-capture events collected since 0.7; requested counts are not historical captures','views','One gallery opening per document; no unique-person claim','image_views','Loaded image observed in viewport, once per asset and document','downloads','Download clicks, not confirmed completed files','generation_ms','OpenAI request through complete response body; excludes Storage finalization','transfer_ms','Booth request through complete body, excludes waiting for a transfer slot','unknown_source','Historical/unclassified sessions remain included unless identified as fixtures'),
  'totals',(select jsonb_build_object('sessions',count(*) filter(where kind='session_started'),'captured',count(*) filter(where kind='capture_saved'),
    'generated',count(*) filter(where kind='generation' and outcome='success'),'generation_failures',count(*) filter(where kind='generation' and outcome<>'success'),
    'gallery_views',count(*) filter(where kind='gallery_view'),'sessions_viewed',count(distinct session_id) filter(where kind='gallery_view'),
    'image_views',count(*) filter(where kind='image_view'),'download_requests',count(*) filter(where kind='download_requested'),
    'sessions_ready',count(*) filter(where kind='session_ready'),'sessions_failed',count(*) filter(where kind='session_failed'),
    'avg_captures_per_observed_session',(select round(avg(captures),2) from per_session where session_id in(select session_id from events where kind='session_started')),
    'unclassified_events',count(*) filter(where effective_source='unknown')) from events),
  'timings',coalesce((select jsonb_agg(to_jsonb(t)) from timings t),'[]'::jsonb),
  'modes',coalesce((select jsonb_agg(to_jsonb(m)) from modes m),'[]'::jsonb),
  'chosen_themes',coalesce((select jsonb_agg(to_jsonb(t)) from themes t),'[]'::jsonb),
  'resolved_themes',coalesce((select jsonb_agg(to_jsonb(r)) from resolved r),'[]'::jsonb),
  'download_assets',coalesce((select jsonb_agg(to_jsonb(d)) from downloads d),'[]'::jsonb),
  'hourly',coalesce((select jsonb_agg(to_jsonb(h)) from hourly h),'[]'::jsonb)
);
$$;
revoke all on function public.booth_monitoring_report(integer,boolean) from public,anon,authenticated;
grant execute on function public.booth_monitoring_report(integer,boolean) to service_role;
