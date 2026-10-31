-- Private generation inputs are separate from archived originals. Existing
-- names, grants, RLS and immutable asset hashes retain their current behavior.
alter table public.booth_assets drop constraint if exists booth_assets_name_check;
alter table public.booth_assets add constraint booth_assets_name_check
  check (name ~ '^((photo|original|input)-[1-4]\.jpg|sheet\.jpg|archive\.zip)$');
