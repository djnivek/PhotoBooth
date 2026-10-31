(function (root, factory) {
  const model = factory();
  if (typeof module === 'object' && module.exports) module.exports = model;
  else root.ReportModel = model;
})(typeof globalThis === 'object' ? globalThis : this, function () {
  'use strict';
  const locale = 'fr-FR'; const zone = 'Europe/Paris';
  const number = value => typeof value === 'number' && Number.isFinite(value) && value >= 0;
  const count = value => number(value) ? new Intl.NumberFormat(locale, {maximumFractionDigits: 2}).format(value) : 'Non mesuré';
  const duration = value => !number(value) ? 'Non mesuré' : value < 1000 ? `${count(Math.round(value))} ms` : `${count(Math.round(value / 100) / 10)} s`;
  function bytes(value) {
    if (!number(value)) return 'Non mesuré';
    const units = ['o', 'Ko', 'Mo', 'Go', 'To'];
    const index = Math.min(4, Math.max(0, Math.floor(Math.log(value || 1) / Math.log(1024))));
    return `${count(Math.round(value / 1024 ** index * 10) / 10)} ${units[index]}`;
  }
  function date(value, time = true) {
    if (!value || !Number.isFinite(Date.parse(value))) return 'Non mesuré';
    return new Intl.DateTimeFormat(locale, {timeZone: zone, day: 'numeric', month: 'short', ...(time ? {hour: '2-digit', minute: '2-digit'} : {})}).format(new Date(value));
  }
  const names = {chef: 'La Surprise du Chef', 'colors-party': 'Colors Party', colors_party: 'Colors Party', party: 'Colors Party', halloween: 'Halloween', space: 'Dans les étoiles', fantasy: 'Pays imaginaire', elves: 'Petits lutins', disco: 'Disco velours', original: 'Au naturel', surprise: 'Métamorphose'};
  const label = value => value == null ? 'Non renseigné' : names[value] || String(value);
  function validate(report) {
    if (!report || !Number.isInteger(report.days) || report.days < 1 || report.days > 366 || typeof report.include_fixtures !== 'boolean' || !Number.isFinite(Date.parse(report.generated_at)) || !report.totals || Array.isArray(report.totals)) throw new Error('invalid_report');
    for (const key of ['sessions', 'captured', 'generated', 'download_requests', 'generation_failures', 'gallery_views', 'sessions_viewed', 'image_views', 'sessions_ready', 'sessions_failed', 'unclassified_events']) if (!number(report.totals[key])) throw new Error('invalid_report');
    for (const key of ['timings', 'modes', 'chosen_themes', 'resolved_themes', 'download_assets', 'hourly']) if (!Array.isArray(report[key])) throw new Error('invalid_report');
    const rows = (key, check) => { if (!report[key].every(row => row && typeof row === 'object' && check(row))) throw new Error('invalid_report'); };
    rows('timings', row => ['upload', 'download', 'generation'].includes(row.kind) && ['successful', 'timed_samples', 'unsuccessful', 'bytes'].every(key => number(row[key])) && ['avg_ms', 'p50_ms', 'p95_ms'].every(key => row[key] === null || number(row[key])));
    rows('modes', row => typeof row.mode === 'string' && number(row.sessions) && number(row.requested_photos));
    rows('chosen_themes', row => typeof row.requested_style === 'string' && number(row.sessions));
    rows('resolved_themes', row => (row.style === null || typeof row.style === 'string') && number(row.generated_images));
    rows('download_assets', row => (row.asset === null || typeof row.asset === 'string') && number(row.requests));
    rows('hourly', row => Number.isFinite(Date.parse(row.hour)) && ['sessions', 'captured', 'generated'].every(key => number(row[key])));
    return report;
  }
  function dayKey(timestamp) {
    const parts = new Intl.DateTimeFormat('en-CA', {timeZone: zone, year: 'numeric', month: '2-digit', day: '2-digit'}).formatToParts(new Date(timestamp));
    return ['year', 'month', 'day'].map(key => parts.find(p => p.type === key).value).join('-');
  }
  function activity(report) {
    const end = Date.parse(report.generated_at); const start = end - report.days * 86400000;
    const hourly = report.days === 1; const step = 3600000; const bins = new Map();
    for (let at = Math.floor(start / step) * step; at <= end; at += step) {
      const key = hourly ? String(at) : dayKey(at);
      if (!bins.has(key)) bins.set(key, {key, at, label: hourly ? new Intl.DateTimeFormat(locale, {timeZone: zone, hour: '2-digit', minute: '2-digit'}).format(at) : date(new Date(at).toISOString(), false), sessions: 0, captured: 0, generated: 0});
    }
    for (const row of report.hourly) {
      const at = Date.parse(row.hour);
      if (!Number.isFinite(at) || at > end || at < Math.floor(start / step) * step) continue;
      const bin = bins.get(hourly ? String(Math.floor(at / step) * step) : dayKey(at));
      if (bin) for (const key of ['sessions', 'captured', 'generated']) if (number(row[key])) bin[key] += row[key];
    }
    return [...bins.values()];
  }
  function downloadGroups(rows) {
    const result = {originals: 0, transformed: 0, sheets: 0, archives: 0, other: 0};
    for (const row of rows) {
      const name = row.asset || '';
      const group = /^original-[1-4]\.jpg$/.test(name) ? 'originals' : /^photo-[1-4]\.jpg$/.test(name) ? 'transformed' : name === 'sheet.jpg' ? 'sheets' : name === 'archive.zip' ? 'archives' : 'other';
      if (number(row.requests)) result[group] += row.requests;
    }
    return result;
  }
  return {number, count, duration, bytes, date, label, validate, activity, downloadGroups};
});
