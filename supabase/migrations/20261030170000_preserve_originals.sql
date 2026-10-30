-- Retain every unedited pose alongside its result, including in the download ZIP.
alter table public.booth_assets drop constraint booth_assets_name_check;
alter table public.booth_assets add constraint booth_assets_name_check
  check (name ~ '^((photo|original)-[1-4]\.jpg|sheet\.jpg|archive\.zip)$');
alter table public.booth_assets drop constraint booth_assets_bytes_check;
alter table public.booth_assets add constraint booth_assets_bytes_check
  check (bytes between 1 and 52428800);
update storage.buckets set file_size_limit=52428800 where id='booth-private';
