// Synthetic, aggregate-only data for UI/security rehearsal. Never used by the production entrypoint.
module.exports = function sample(days = 7, include_fixtures = false) {
  const end = new Date(); end.setMinutes(0, 0, 0);
  const hourly = [2,4,8,13,18,24,20,15,10,6].map((sessions,index) => ({hour:new Date(end.getTime() - (9-index)*3600000).toISOString(),sessions,captured:sessions*3,generated:sessions*2}));
  return {
    generated_at: new Date().toISOString(), collection_started_at: '2026-10-31T10:54:08Z', days, include_fixtures,
    totals:{sessions:120,captured:360,generated:240,generation_failures:3,download_requests:186,gallery_views:172,sessions_viewed:98,image_views:604,sessions_ready:115,sessions_failed:2,avg_captures_per_observed_session:3,unclassified_events:1},
    hourly,
    timings:[{kind:'upload',successful:360,timed_samples:360,unsuccessful:2,avg_ms:1230,p50_ms:1100,p95_ms:2840,bytes:987654321},{kind:'generation',successful:240,timed_samples:239,unsuccessful:3,avg_ms:58400,p50_ms:52200,p95_ms:94600,bytes:0},{kind:'download',successful:240,timed_samples:240,unsuccessful:1,avg_ms:1420,p50_ms:1200,p95_ms:3510,bytes:489654321}],
    modes:[{mode:'original',sessions:40,requested_photos:120},{mode:'surprise',sessions:80,requested_photos:240}],
    chosen_themes:[{requested_style:'chef',sessions:32},{requested_style:'halloween',sessions:18},{requested_style:'colors_party',sessions:14},{requested_style:'space',sessions:9},{requested_style:'disco',sessions:7}],
    resolved_themes:[{style:'halloween',generated_images:66},{style:'colors_party',generated_images:60},{style:'space',generated_images:36},{style:'disco',generated_images:33},{style:'fantasy',generated_images:27},{style:'elves',generated_images:18}],
    download_assets:[{asset:'original-1.jpg',requests:30},{asset:'photo-1.jpg',requests:72},{asset:'sheet.jpg',requests:24},{asset:'archive.zip',requests:60}],
    operations:{observed_at:new Date().toISOString(),last_event_received_at:new Date().toISOString(),budget:{started:243,max_images:2400,generation_enabled:true,max_per_minute:16,max_in_flight:8},jobs:{queued:2,starting:1,running:3,ready:240,failed:2,uncertain:1,active_over_five_minutes:1},storage:{files:986,known_bytes:1934876543,unknown_size_files:0}}
  };
};
