(() => {
  'use strict';
  const M = window.ReportModel;
  const $ = id => document.getElementById(id);
  const base = document.querySelector('meta[name="admin-base"]').content;
  let authenticated = false; let current = null; let flight = null; let timer = null; let revision = 0;
  const text = (id, value) => { $(id).textContent = value; };
  const el = (tag, content, className) => { const node = document.createElement(tag); if (content != null) node.textContent = content; if (className) node.className = className; return node; };
  function errorAt(id, message) { text(id, message || ''); $(id).hidden = !message; }
  async function json(route, options = {}) {
    const response = await fetch(base + route, {credentials: 'same-origin', cache: 'no-store', ...options});
    if (!response.ok) { const error = new Error('request_failed'); error.status = response.status; throw error; }
    return response.json();
  }
  function signInScreen(message = '') {
    authenticated = false; current = null; revision++; clearTimeout(timer); flight?.abort(); flight = null;
    $('dashboard').hidden = true; $('report').hidden = true; $('logout').hidden = true; $('boot').hidden = true; $('login').hidden = false;
    $('code').value = ''; $('code').type = 'password'; $('code-file').value = '';
    $('reveal').setAttribute('aria-pressed', 'false'); $('reveal').setAttribute('aria-label', 'Afficher le code admin'); text('reveal', 'Afficher');
    errorAt('login-error', message); $('code').focus();
  }
  function schedule() { clearTimeout(timer); if (authenticated && !document.hidden) timer = setTimeout(() => refresh(), 30000); }
  function showDashboard() {
    authenticated = true; $('boot').hidden = true; $('login').hidden = true; $('dashboard').hidden = false; $('logout').hidden = false;
    $('code').value = ''; $('code-file').value = ''; $('dashboard-title').focus(); refresh();
  }
  $('reveal').addEventListener('click', () => {
    const show = $('code').type === 'password'; $('code').type = show ? 'text' : 'password';
    $('reveal').setAttribute('aria-pressed', String(show)); $('reveal').setAttribute('aria-label', show ? 'Masquer le code admin' : 'Afficher le code admin'); text('reveal', show ? 'Masquer' : 'Afficher');
  });
  $('code-file').addEventListener('change', async event => {
    const file = event.target.files[0]; if (!file) return;
    if (file.size > 1024) { errorAt('login-error', 'Ce fichier ne contient pas un code admin valide.'); event.target.value = ''; return; }
    try {
      const code = (await file.text()).trim();
      if (!/^[a-f0-9]{64}$/.test(code)) throw new Error('invalid_code_file');
      $('code').value = code; errorAt('login-error', ''); $('login-button').focus();
    } catch { errorAt('login-error', 'Impossible de lire ce code. Sélectionnez le fichier access-token du Mac.'); }
    event.target.value = '';
  });
  $('login-form').addEventListener('submit', async event => {
    event.preventDefault(); $('login-button').disabled = true; text('login-button', 'Connexion…'); errorAt('login-error', '');
    try {
      await json('/api/session', {method: 'POST', signal: AbortSignal.timeout(15000), headers: {'Content-Type': 'application/json', 'X-Photobooth-Admin': '1'}, body: JSON.stringify({token: $('code').value.trim()})});
      showDashboard();
    } catch (error) {
      errorAt('login-error', error.status === 401 ? 'Ce code ne correspond pas au code admin. Vérifiez le fichier du Mac.' : error.status === 429 ? 'Plusieurs tentatives ont échoué. Patientez une minute avant de réessayer.' : 'Connexion impossible. Vérifiez que le Mac et Tailscale sont accessibles, puis réessayez.');
    } finally { $('login-button').disabled = false; text('login-button', 'Ouvrir le tableau de bord'); }
  });
  $('logout').addEventListener('click', async () => {
    $('logout').disabled = true;
    try {
      await json('/api/logout', {method: 'POST', signal: AbortSignal.timeout(15000), headers: {'X-Photobooth-Admin': '1'}});
      signInScreen();
    } catch (error) {
      if (error.status === 401) signInScreen();
      else errorAt('report-error', 'Déconnexion non confirmée. Rétablissez la connexion puis réessayez.');
    } finally { $('logout').disabled = false; }
  });
  $('filters').addEventListener('submit', event => { event.preventDefault(); refresh(true); });
  for (const id of ['period', 'fixtures']) $(id).addEventListener('change', () => refresh(true));
  document.addEventListener('visibilitychange', () => { if (document.hidden) clearTimeout(timer); else if (authenticated) refresh(); });
  window.addEventListener('pagehide', () => { clearTimeout(timer); flight?.abort(); });
  window.addEventListener('pageshow', event => { if (event.persisted) { signInScreen(); checkSession(); } });
  function list(id, items) {
    $(id).replaceChildren(...items.map(([name, value]) => {
      const row = el('div'); row.append(el('dt', name), el('dd', value)); return row;
    }));
  }
  function breakdown(id, rows, empty) {
    const target = $(id); target.replaceChildren();
    if (!rows.length || rows.every(row => !row.value)) { target.append(el('p', empty, 'empty')); return; }
    const max = Math.max(1, ...rows.map(row => row.value || 0));
    for (const row of [...rows].sort((a, b) => b.value - a.value)) {
      const item = el('div', null, 'breakdown-row');
      item.append(el('span', row.label, 'breakdown-label'), el('strong', M.count(row.value)));
      const bar = el('progress'); bar.max = max; bar.value = row.value; bar.setAttribute('aria-hidden', 'true'); item.append(bar); target.append(item);
    }
  }
  function chart(report) {
    const bins = M.activity(report); const maximum = Math.max(1, ...bins.flatMap(b => [b.sessions, b.captured, b.generated]));
    const any = bins.some(b => b.sessions || b.captured || b.generated);
    $('activity-empty').hidden = any; $('chart').hidden = !any;
    text('activity-unit', report.days === 1 ? 'Par heure' : 'Par jour');
    text('range', `${M.date(new Date(Date.parse(report.generated_at) - report.days * 86400000).toISOString())} au ${M.date(report.generated_at)} · heure de Paris`);
    $('activity-rows').replaceChildren(...bins.map(bin => { const tr = el('tr'); const th = el('th', M.date(new Date(bin.at).toISOString(), report.days === 1)); th.scope = 'row'; tr.append(th, ...['sessions', 'captured', 'generated'].map(key => el('td', M.count(bin[key])))); return tr; }));
    const svgEl = (tag, attrs, content) => { const node = document.createElementNS('http://www.w3.org/2000/svg', tag); for (const [key, value] of Object.entries(attrs)) node.setAttribute(key, value); if (content != null) node.textContent = content; return node; };
    const svg = svgEl('svg', {viewBox: '0 0 640 228', 'aria-hidden': 'true'});
    for (const y of [0, 1]) { const level = 187 - y * 160; svg.append(svgEl('line', {x1: 40, y1: level, x2: 634, y2: level, class: 'gridline'}), svgEl('text', {x: 30, y: level + 4, 'text-anchor': 'end'}, M.count(y * maximum))); }
    const width = 590 / bins.length; const barWidth = Math.min(13, (width - 4) / 3);
    bins.forEach((bin, index) => {
      const center = 40 + width * (index + .5);
      ['sessions', 'captured', 'generated'].forEach((key, series) => {
        const height = bin[key] / maximum * 160;
        const bar = svgEl('rect', {x: center + (series - 1.5) * barWidth, y: 187 - height, width: Math.max(1, barWidth - 1), height, rx: 1, class: 'bar-' + key});
        bar.append(svgEl('title', {}, `${bin.label} : ${({sessions:'séances',captured:'photos prises',generated:'métamorphoses'})[key]} ${M.count(bin[key])}`)); svg.append(bar);
      });
      if (index === 0 || index === bins.length - 1 || (index % Math.ceil(bins.length / 4) === 0 && index < bins.length - Math.ceil(bins.length / 8))) svg.append(svgEl('text', {x: center, y: 214, 'text-anchor': index === 0 ? 'start' : index === bins.length - 1 ? 'end' : 'middle'}, bin.label));
    });
    $('chart').replaceChildren(svg);
  }
  function operations(report) {
    const op = report.operations; $('operations-unavailable').hidden = !!op; $('operations-data').hidden = !op;
    if (!op) return;
    const budget = op.budget; const jobs = op.jobs || {}; const storage = op.storage || {};
    const active = M.number(jobs.starting) && M.number(jobs.running) ? jobs.starting + jobs.running : null;
    const available = budget && budget.generation_enabled && budget.started < budget.max_images;
    text('generation-state', !budget ? 'Limites non disponibles' : !budget.generation_enabled ? 'Générations suspendues' : budget.started >= budget.max_images ? 'Plafond de l’événement atteint' : 'Générations autorisées');
    $('generation-state').classList.toggle('attention', !available);
    text('budget-count', budget ? `${M.count(budget.started)} / ${M.count(budget.max_images)}` : 'Non mesuré');
    $('budget').hidden = !budget; $('budget').max = Math.max(1, budget?.max_images || 0); $('budget').value = budget?.started || 0;
    text('jobs-active', M.count(active)); text('jobs-uncertain', M.count(jobs.uncertain)); text('storage-files', M.count(storage.files)); text('storage-bytes', M.bytes(storage.known_bytes));
    text('storage-note', storage.unknown_size_files ? `${M.count(storage.unknown_size_files)} ${storage.unknown_size_files === 1 ? 'fichier de taille inconnue' : 'fichiers de taille inconnue'}. Le volume affiché est partiel.` : 'Photos et archives du photomaton. Quota du forfait non mesuré.');
    const warnings = [];
    if (jobs.uncertain) warnings.push(`${M.count(jobs.uncertain)} ${jobs.uncertain === 1 ? 'résultat incertain' : 'résultats incertains'} à vérifier avant toute relance.`);
    if (jobs.active_over_five_minutes) warnings.push(`${M.count(jobs.active_over_five_minutes)} ${jobs.active_over_five_minutes === 1 ? 'traitement' : 'traitements'} sans mise à jour depuis plus de 5 minutes.`);
    if (budget && M.number(active) && active + (jobs.uncertain || 0) >= budget.max_in_flight) warnings.push('Toutes les places de génération sont occupées.');
    errorAt('operations-warning', warnings.join(' '));
    list('operation-details', [['Départs maximum par minute', M.count(budget?.max_per_minute)], ['Places simultanées', M.count(budget?.max_in_flight)], ['En attente', M.count(jobs.queued)], ['Terminés', M.count(jobs.ready)], ['En échec', M.count(jobs.failed)], ['État relevé à', M.date(op.observed_at)]]);
  }
  function render(report) {
    const t = report.totals;
    for (const key of ['sessions', 'captured', 'generated', 'download_requests']) text('stat-' + key, M.count(t[key]));
    text('freshness', `Données relevées le ${M.date(report.generated_at)} · heure de Paris`);
    const missingStarts = !t.sessions && (t.generated || t.gallery_views || t.captured);
    const coverage = [];
    if (t.unclassified_events) coverage.push(`${M.count(t.unclassified_events)} ${t.unclassified_events === 1 ? 'événement de source non classée est inclus' : 'événements de source non classée sont inclus'} dans ces chiffres.`);
    if (missingStarts) coverage.push('Des événements existent sans départ de séance enregistré. Le suivi précis de la borne nécessite la version 0.7.');
    errorAt('coverage', coverage.join(' '));
    chart(report); operations(report);
    const kinds = [['upload', 'Envoi des photos'], ['generation', 'Génération OpenAI'], ['download', 'Réception sur la borne']];
    $('timing-rows').replaceChildren(...kinds.map(([kind, label]) => {
      const row = report.timings.find(timing => timing.kind === kind);
      const tr = el('tr'); const th = el('th', label); th.scope = 'row';
      tr.append(th, ...[M.duration(row?.avg_ms), M.duration(row?.p50_ms), M.duration(row?.p95_ms), row ? `${M.count(row.timed_samples)} / ${M.count(row.successful)}` : '0 / 0', M.count(row?.unsuccessful ?? 0), row?.kind === 'generation' ? 'Non suivi' : row && row.timed_samples ? M.bytes(row.bytes) : 'Non mesuré'].map(value => el('td', value)));
      return tr;
    }));
    breakdown('modes', report.modes.map(row => ({label: M.label(row.mode), value: row.sessions})), 'Aucun départ de séance enregistré sur cette période.');
    list('session-details', [['Résultats prêts', M.count(t.sessions_ready)], ['Séances en échec', M.count(t.sessions_failed)], ['Générations non réussies', M.count(t.generation_failures)], ['Captures par séance observée', M.count(t.avg_captures_per_observed_session)], ['Photos demandées au départ', M.count(report.modes.reduce((sum, row) => sum + (row.requested_photos || 0), 0))]]);
    list('sharing-details', [['Ouvertures de galerie', M.count(t.gallery_views)], ['Séances consultées', M.count(t.sessions_viewed)], ['Images vues', M.count(t.image_views)]]);
    const groups = M.downloadGroups(report.download_assets);
    breakdown('downloads', [['originals', 'Originaux'], ['transformed', 'Photos Métamorphose'], ['sheets', 'Planches à imprimer'], ['archives', 'Archives complètes'], ['other', 'Autres fichiers']].filter(([key]) => groups[key]).map(([key, label]) => ({label, value: groups[key]})), 'Aucun clic de téléchargement enregistré sur cette période.');
    breakdown('chosen-themes', report.chosen_themes.map(row => ({label: M.label(row.requested_style), value: row.sessions})), 'Les univers choisis apparaîtront après les premiers départs Métamorphose.');
    breakdown('resolved-themes', report.resolved_themes.map(row => ({label: M.label(row.style), value: row.generated_images})), 'Les univers apparaîtront après les premières images générées.');
    text('collection', report.collection_started_at ? `Collecte commencée le ${M.date(report.collection_started_at)}. Les captures et transferts nécessitent la borne 0.7.` : 'La collecte commencera à la réception du premier événement. Les captures et transferts nécessitent la borne 0.7.');
    text('last-event', report.operations?.last_event_received_at ? `Dernière mesure reçue le ${M.date(report.operations.last_event_received_at)}. Ce repère ne confirme pas que la borne est connectée.` : 'Aucune date de dernière mesure disponible.');
    $('export').href = `${base}/api/report?days=${report.days}&fixtures=${report.include_fixtures ? 1 : 0}&download=1`; $('export').hidden = false;
    $('report').hidden = false;
  }
  async function refresh(replace = false) {
    if (!authenticated || (flight && !replace)) return;
    clearTimeout(timer); flight?.abort(); const controller = new AbortController(); flight = controller; const id = ++revision;
    const timeout = setTimeout(() => controller.abort(), 30000);
    const days = Number($('period').value); const fixtures = $('fixtures').checked;
    $('refresh').disabled = true; text('refresh', 'Actualisation…'); $('loading').hidden = !!current; $('dashboard').setAttribute('aria-busy', 'true');
    try {
      const report = M.validate(await json(`/api/report?days=${days}&fixtures=${fixtures ? 1 : 0}`, {signal: controller.signal}));
      if (id !== revision || !authenticated) return;
      if (report.days !== days || report.include_fixtures !== fixtures) throw new Error('report_filter_mismatch');
      render(report); current = report; errorAt('report-error', '');
    } catch (error) {
      if (id !== revision || !authenticated) return;
      if (error.status === 401) return signInScreen('Votre session a expiré. Saisissez à nouveau le code admin.');
      if (current) {
        $('period').value = String(current.days); $('fixtures').checked = current.include_fixtures;
        text('freshness', `Dernières données disponibles : ${M.date(current.generated_at)} · heure de Paris`);
      }
      errorAt('report-error', current ? 'Actualisation impossible. Les derniers chiffres sont conservés, avec leur période d’origine. Vérifiez la connexion puis appuyez sur Actualiser.' : 'Les statistiques sont indisponibles. Vérifiez la connexion du Mac et son accès Supabase, puis appuyez sur Actualiser.');
    } finally {
      clearTimeout(timeout);
      if (id === revision) { flight = null; $('refresh').disabled = false; text('refresh', 'Actualiser'); $('loading').hidden = true; $('dashboard').setAttribute('aria-busy', 'false'); schedule(); }
    }
  }
  async function checkSession() {
    try { await json('/api/session', {signal: AbortSignal.timeout(15000)}); showDashboard(); }
    catch (error) { signInScreen(error.status === 401 ? '' : 'Le serveur admin est inaccessible. Vérifiez Tailscale et le Mac, puis réessayez.'); }
  }
  checkSession();
})();
