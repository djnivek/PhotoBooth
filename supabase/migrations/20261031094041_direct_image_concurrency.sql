-- Two overlapping four-pose sessions. Preserve the event budget, consumption
-- counter, operator kill switch and all existing transactional claim checks.
update public.booth_budget
set max_in_flight = 8, max_per_minute = 16
where id = 'event';
