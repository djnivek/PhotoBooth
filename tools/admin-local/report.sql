-- Fixed aggregate-only query. The two placeholders are validated by the server.
select public.booth_monitoring_report(__DAYS__, __FIXTURES__) || jsonb_build_object(
  'operations', jsonb_build_object(
    'observed_at', now(),
    'last_event_received_at', (select max(received_at) from public.booth_metrics),
    'budget', (select jsonb_build_object('started', started, 'max_images', max_images,
      'generation_enabled', generation_enabled, 'max_per_minute', max_per_minute,
      'max_in_flight', max_in_flight) from public.booth_budget where id = 'event'),
    'jobs', (select jsonb_build_object(
      'queued', count(*) filter (where state = 'queued'),
      'starting', count(*) filter (where state = 'starting'),
      'running', count(*) filter (where state = 'running'),
      'ready', count(*) filter (where state = 'ready'),
      'failed', count(*) filter (where state = 'failed'),
      'uncertain', count(*) filter (where state = 'uncertain'),
      'active_over_five_minutes', count(*) filter (where state in ('starting', 'running') and updated_at < now() - interval '5 minutes')
    ) from public.booth_jobs),
    'storage', (select jsonb_build_object(
      'files', count(*),
      'known_bytes', coalesce(sum(case when metadata->>'size' ~ '^[0-9]{1,20}$' then (metadata->>'size')::numeric end), 0),
      'unknown_size_files', count(*) filter (where metadata->>'size' is null or not (metadata->>'size' ~ '^[0-9]{1,20}$'))
    ) from storage.objects where bucket_id = 'booth-private')
  )
) as report;
