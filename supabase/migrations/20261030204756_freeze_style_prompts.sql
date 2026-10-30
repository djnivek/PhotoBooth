-- Freeze a session's creative instructions so editing the catalog cannot change later poses.
alter table public.booth_sessions
  add column requested_style text not null default 'chef',
  add column style_prompt text;
