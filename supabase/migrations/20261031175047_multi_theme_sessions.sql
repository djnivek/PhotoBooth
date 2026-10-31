-- NULL preserves the scalar prompt and semantics of sessions created by older servers.
-- The API inserts the complete immutable plan atomically with the session.
alter table public.booth_sessions
  add column requested_styles text[],
  add column shot_styles jsonb,
  add constraint booth_requested_styles_size check (
    requested_styles is null or (cardinality(requested_styles) between 1 and photo_count
      and array_position(requested_styles, null) is null)),
  add constraint booth_shot_styles_size check (
    shot_styles is null or (mode = 'surprise' and requested_styles is not null
      and jsonb_typeof(shot_styles) = 'array' and jsonb_array_length(shot_styles) = photo_count));

comment on column public.booth_sessions.shot_styles is
  'Private, frozen per-pose theme IDs and complete prompts. Only IDs are returned to the booth.';
