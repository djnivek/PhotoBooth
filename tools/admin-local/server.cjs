// Private, aggregate-only administration. Provider credentials stay in the local CLI.
const http = require('node:http');
const fs = require('node:fs/promises');
const path = require('node:path');
const os = require('node:os');
const crypto = require('node:crypto');
const {execFile} = require('node:child_process');
const {promisify} = require('node:util');
const run = promisify(execFile);
const root = path.resolve(__dirname, '../..');
const project = 'frkckrvsdsiwivdfyjau';
const cookieName = 'photobooth_admin';
function equal(a, b) {
  const x = Buffer.from(a || ''); const y = Buffer.from(b);
  return x.length === y.length && crypto.timingSafeEqual(x, y);
}
async function loadOrCreateToken(file) {
  await fs.mkdir(path.dirname(file), {recursive: true, mode: 0o700});
  try { await fs.writeFile(file, crypto.randomBytes(32).toString('hex'), {flag: 'wx', mode: 0o600}); }
  catch (error) { if (error.code !== 'EEXIST') throw error; }
  const stat = await fs.lstat(file);
  if (!stat.isFile() || stat.isSymbolicLink()) throw new Error('Admin code must be a regular file');
  const token = (await fs.readFile(file, 'utf8')).trim();
  if (!/^[a-f0-9]{64}$/.test(token)) throw new Error('Invalid local admin code; refusing to replace it');
  await fs.chmod(file, 0o600);
  return token;
}
async function readReport(days, fixtures, {cli = process.env.PHOTOBOOTH_SUPABASE_CLI || 'supabase', cwd = root, execute = run} = {}) {
  if (!Number.isInteger(days) || days < 1 || days > 366 || typeof fixtures !== 'boolean') throw new Error('Invalid report range');
  const linkedProject = (await fs.readFile(path.join(cwd, 'supabase/.temp/project-ref'), 'utf8')).trim();
  if (linkedProject !== project) throw new Error('Link this checkout to the PhotoBooth project before reading reports');
  const template = await fs.readFile(path.join(__dirname, 'report.sql'), 'utf8');
  const directory = await fs.mkdtemp(path.join(os.tmpdir(), 'photobooth-admin-'));
  try {
    const file = path.join(directory, 'report.sql');
    await fs.writeFile(file, template.replace('__DAYS__', String(days)).replace('__FIXTURES__', String(fixtures)));
    const {stdout} = await execute(cli, ['db', 'query', '--linked', '--file', file, '--output', 'json'], {cwd, windowsHide: true, timeout: 25000, maxBuffer: 4 * 1024 * 1024});
    const output = JSON.parse(stdout); const rows = Array.isArray(output) ? output : output?.rows;
    const result = rows?.[0]?.report;
    if (!result?.totals || !Array.isArray(result.timings)) throw new Error('Invalid report response');
    return result;
  } finally {
    await fs.unlink(path.join(directory, 'report.sql')).catch(() => {});
    await fs.rmdir(directory).catch(() => {});
  }
}
function createAdminServer({token, read = readReport, tailnetHost, basePath = '', now = Date.now, sessionMs = 12 * 60 * 60 * 1000} = {}) {
  if (typeof token !== 'string' || token.length < 32) throw new Error('Strong local token required');
  if (tailnetHost && !/^[a-z0-9-]+\.[a-z0-9.-]+\.ts\.net$/.test(tailnetHost)) throw new Error('Use the exact Tailscale .ts.net hostname');
  if (basePath && !/^\/[a-z0-9-]+$/.test(basePath)) throw new Error('Invalid admin base path');
  const basic = 'Basic ' + Buffer.from('admin:' + token).toString('base64');
  const cache = new Map(); const inFlight = new Map(); const sessions = new Map();
  let failures = 0; let blockedUntil = 0;
  const assets = new Map([
    ['/style.css', ['web/admin/style.css', 'text/css; charset=utf-8']],
    ['/dashboard.js', ['web/admin/dashboard.js', 'text/javascript; charset=utf-8']],
    ['/report-model.js', ['web/admin/report-model.js', 'text/javascript; charset=utf-8']],
    ['/assets/wordmark.png', ['web/gallery/assets/wordmark.png', 'image/png']],
    ['/assets/BricolageGrotesque.ttf', ['web/gallery/assets/BricolageGrotesque.ttf', 'font/ttf']]
  ]);
  const server = http.createServer(async (req, res) => {
    const send = (status, body, type = 'application/json; charset=utf-8', extra = {}) => {
      res.writeHead(status, {'Content-Type': type, 'Cache-Control': 'no-store', 'Vary': 'Authorization, Cookie, Accept',
        'X-Content-Type-Options': 'nosniff', 'Referrer-Policy': 'no-referrer', 'X-Frame-Options': 'DENY',
        'Content-Security-Policy': "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'",
        'Permissions-Policy': 'camera=(), microphone=(), geolocation=()', ...extra});
      res.end(type.startsWith('application/json') ? JSON.stringify(body) : body);
    };
    try {
      const port = server.address()?.port;
      const hosts = new Set(['127.0.0.1:' + port, 'localhost:' + port, ...(tailnetHost ? [tailnetHost] : [])]);
      if (!hosts.has(req.headers.host)) return send(403, {error: 'host_not_allowed'});
      const secure = !!tailnetHost && req.headers.host === tailnetHost;
      const origin = (secure ? 'https://' : 'http://') + req.headers.host;
      if (req.headers.origin && req.headers.origin !== origin) return send(403, {error: 'origin_not_allowed'});
      if (req.headers['sec-fetch-site'] === 'cross-site') return send(403, {error: 'origin_not_allowed'});
      const url = new URL(req.url, 'http://127.0.0.1');
      let route = url.pathname;
      // Serve strips its configured prefix; direct local testing may retain it.
      if (basePath && (route === basePath || route.startsWith(basePath + '/'))) route = route.slice(basePath.length) || '/';
      if (req.method === 'GET' && route === '/api/report' && req.headers.accept?.includes('text/html') && url.searchParams.get('download') !== '1') {
        return send(303, '', 'text/plain', {Location: (basePath || '') + '/'});
      }
      if (req.method === 'GET' && (route === '/' || assets.has(route))) {
        if (route === '/') {
          const html = (await fs.readFile(path.join(root, 'web/admin/index.html'), 'utf8')).replaceAll('__BASE__', basePath);
          return send(200, html, 'text/html; charset=utf-8');
        }
        const [file, type] = assets.get(route);
        return send(200, await fs.readFile(path.join(root, file)), type);
      }
      for (const [id, expires] of sessions) if (expires <= now()) sessions.delete(id);
      const sessionId = (req.headers.cookie || '').split(';').map(s => s.trim()).find(s => s.startsWith(cookieName + '='))?.slice(cookieName.length + 1);
      const authorized = equal(req.headers.authorization, 'Bearer ' + token) || equal(req.headers.authorization, basic) || sessions.has(sessionId);
      const cookie = (id, age) => `${cookieName}=${id}; Path=${basePath || '/'}; HttpOnly; SameSite=Strict; Max-Age=${age}${secure ? '; Secure' : ''}`;
      if (route === '/api/session' && req.method === 'POST') {
        if (req.headers.origin !== origin || req.headers['x-photobooth-admin'] !== '1') return send(403, {error: 'origin_required'});
        if (!req.headers['content-type']?.startsWith('application/json')) return send(415, {error: 'json_required'});
        if (blockedUntil > now()) return send(429, {error: 'login_limited'}, undefined, {'Retry-After': String(Math.ceil((blockedUntil - now()) / 1000))});
        let bytes = 0; const chunks = [];
        for await (const chunk of req) {
          bytes += chunk.length;
          if (bytes > 2048) return send(413, {error: 'body_too_large'});
          chunks.push(chunk);
        }
        let body;
        try { body = JSON.parse(Buffer.concat(chunks).toString()); } catch { return send(400, {error: 'invalid_json'}); }
        if (typeof body?.token !== 'string' || !equal(body.token, token)) {
          if (++failures >= 5) { blockedUntil = now() + 60000; failures = 0; }
          return send(401, {error: 'invalid_code'});
        }
        failures = 0;
        if (sessionId) sessions.delete(sessionId);
        if (sessions.size >= 32) sessions.delete(sessions.keys().next().value);
        const id = crypto.randomBytes(32).toString('hex'); sessions.set(id, now() + sessionMs);
        return send(200, {authenticated: true}, undefined, {'Set-Cookie': cookie(id, Math.floor(sessionMs / 1000))});
      }
      if (!authorized) return send(401, {error: 'admin_login_required'});
      if (route === '/api/logout' && req.method === 'POST') {
        if (req.headers.origin !== origin || req.headers['x-photobooth-admin'] !== '1') return send(403, {error: 'origin_required'});
        sessions.delete(sessionId);
        return send(200, {authenticated: false}, undefined, {'Set-Cookie': cookie('', 0)});
      }
      if (req.method !== 'GET') return send(405, {error: 'read_only'});
      if (route === '/api/session') return send(200, {authenticated: true});
      if (route === '/health') return send(200, {service: 'photobooth-admin', read_only: true, project});
      if (route !== '/api/report') return send(404, {error: 'not_found'});
      const days = Number(url.searchParams.get('days') ?? '7'); const f = url.searchParams.get('fixtures') ?? '0';
      if (!Number.isInteger(days) || days < 1 || days > 366 || !['0', '1'].includes(f)) return send(400, {error: 'invalid_range'});
      const key = days + ':' + f;
      const attachment = url.searchParams.get('download') === '1' ? {'Content-Disposition': 'attachment; filename="colors-party-rapport.json"'} : {};
      try {
        const old = cache.get(key);
        if (old && now() - old.at < 15000) return send(200, old.report, undefined, attachment);
        if (!inFlight.has(key)) {
          if (inFlight.size >= 4) return send(429, {error: 'report_busy'}, undefined, {'Retry-After': '5'});
          inFlight.set(key, Promise.resolve().then(() => read(days, f === '1')).finally(() => inFlight.delete(key)));
        }
        const report = await inFlight.get(key);
        if (cache.size >= 4 && !cache.has(key)) cache.delete(cache.keys().next().value);
        cache.set(key, {at: now(), report});
        return send(200, report, undefined, attachment);
      } catch { return send(503, {error: 'supabase_report_unavailable'}); }
    } catch { if (!res.headersSent) send(500, {error: 'admin_unavailable'}); else res.end(); }
  });
  server.requestTimeout = 15000; server.headersTimeout = 10000;
  return server;
}
if (require.main === module) {
  (async () => {
    const file = path.join(root, 'artifacts/admin-local/access-token');
    const token = await loadOrCreateToken(file);
    const server = createAdminServer({token, tailnetHost: process.env.PHOTOBOOTH_ADMIN_HOST, basePath: process.env.PHOTOBOOTH_ADMIN_BASE_PATH || ''});
    const port = Number(process.env.PHOTOBOOTH_ADMIN_PORT || 8787);
    if (!Number.isInteger(port) || port < 1024 || port > 65535) throw new Error('Invalid local port');
    server.listen(port, '127.0.0.1', () => console.log('Admin listening on 127.0.0.1:' + port + '; local code file: ' + file));
    const close = () => server.close(() => process.exit(0));
    process.on('SIGINT', close); process.on('SIGTERM', close);
  })().catch(() => { console.error('Cannot start local admin. Check the port and local configuration.'); process.exitCode = 1; });
}
module.exports = {createAdminServer, readReport, loadOrCreateToken};
