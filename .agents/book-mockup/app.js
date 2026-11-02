/* Book mockup v2: views over the embedded model. No framework, no network. */
(function () {
  'use strict';

  var $ = function (s, r) { return (r || document).querySelector(s); };
  var esc = function (s) {
    return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c];
    });
  };
  var n = function (v) { return Number(v || 0).toLocaleString('en-US'); };
  var pct = function (a, b) { return b ? Math.round((a / b) * 100) : 0; };
  var TODAY = new Date((D.generated || '2026-11-02') + 'T00:00:00');
  var MODES = { today: 'today', read: 'read', map: 'map', checks: 'checks', module: 'map' };

  /* ---------- model helpers ---------- */
  var IDX = { ids: D.index.ids.split('\n'), titles: D.index.titles.split('\n'), st: D.index.status };
  var lexOf = function (t) { return (D.lexicon && D.lexicon.types && D.lexicon.types[t]) || null; };
  var typeName = function (t) { var l = lexOf(t); return l ? l.label : String(t || '').replace(/-/g, ' '); };
  var typeQuestion = function (t) { var l = lexOf(t); return l ? l.question : ''; };
  var statusWord = function (c) { return { a: 'approved', r: 'reviewed', d: 'draft', x: 'retired' }[c] || 'record'; };
  var typeOf = function (id) { return String(id || '').split('/')[0]; };
  var baseOf = function (id) { return String(id || '').split('#')[0]; };

  function ref(id, label) {
    var base = baseOf(id);
    if (!id) return '';
    var label2 = label || (D.labels[base] && D.labels[base].t) || (base.indexOf('code/') === 0 ? base.split('/').pop() : base);
    var frag = String(id).indexOf('#') > -1 ? ' ' + esc(String(id).split('#')[1]) : '';
    if (D.records[base]) return '<a class="ref" href="#/r/' + encodeURIComponent(base) + '" title="' + esc(typeName(typeOf(base)) + ' · ' + base) + '">' + esc(label2) + '</a>' + frag;
    return '<span class="ref flat" title="' + esc(base) + '">' + esc(label2) + '</span>' + frag;
  }

  function dueLabel(iso) {
    if (!iso) return '';
    var d = new Date(iso + 'T00:00:00');
    var days = Math.round((d - TODAY) / 86400000);
    var short = d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
    if (days < 0) return short + ' · overdue';
    if (days === 0) return short + ' · today';
    if (days === 1) return short + ' · tomorrow';
    return short + ' · in ' + days + ' days';
  }

  function titleOf(id) {
    return (D.labels[id] && D.labels[id].t) || id;
  }

  function meter(a, b, zero) {
    var w = b ? Math.max(0, Math.min(100, Math.round((a / b) * 100))) : 0;
    return '<span class="bar' + (zero ? ' zero' : '') + '"><i style="width:' + w + '%"></i></span>';
  }

  function typeCountsHtml(types, limit) {
    var list = (types || []).slice(0, limit || 20);
    return '<span class="counts">' + list.map(function (t) {
      return '<span title="' + esc(typeName(t[0]) + ' · ' + typeQuestion(t[0])) + '"><b>' + n(t[1]) + '</b> ' + esc(typeName(t[0])) + '</span>';
    }).join('') + '</span>';
  }

  /* ---------- chrome ---------- */
  function setChrome(mode, pageNo, pageTotal) {
    Array.prototype.forEach.call(document.querySelectorAll('.tab'), function (a) {
      if (a.getAttribute('data-mode') === mode) a.setAttribute('aria-current', 'page');
      else a.removeAttribute('aria-current');
    });
    var line = $('#readline');
    if (mode === 'read' && pageNo && pageTotal) {
      line.hidden = false;
      line.style.width = Math.round((pageNo / pageTotal) * 100) + '%';
    } else {
      line.hidden = true;
    }
    $('#tabToday').textContent = n(D.ballots.length + D.metrics.decisionsQueued);
    $('#tabChecks').textContent = n(D.metrics.findings);
    window.scrollTo(0, 0);
  }

  /* ---------- cover ---------- */
  function stat(v, label, cls) {
    return '<div class="stat ' + (cls || '') + '"><b>' + v + '</b><span>' + esc(label) + '</span></div>';
  }

  function cover() {
    var p = D.records['product/nighthall'];
    var m = D.metrics;
    var stand = (p && p.fields && p.fields.summary) || '';
    setChrome('cover');
    return '<section class="cover"><div class="cover-in">' +
      '<div class="edition"><span class="kicker">' + esc(D.edition) + '</span><span class="kicker">' + esc(longDate(D.generated)) + '</span></div>' +
      '<h1>' + esc(D.project.title) + '</h1>' +
      '<p class="stand">' + esc(stand) + '</p>' +
      '<div class="rule"></div>' +
      '<div class="stats">' +
        stat(n(m.records), 'records') +
        stat(n(m.settled), 'settled') +
        stat(n(m.ballots), 'ballots for you') +
        stat(n(m.decisionsQueued), 'decisions queued') +
        stat(n(m.proofRecords), 'proofs', 'blank') +
      '</div>' +
      '<div class="entries">' +
        entry('#/today', 'Start with what needs you', m.ballots + ' ballots and ' + n(m.decisionsQueued) + ' decisions, then the checks that name a gap.') +
        entry('#/read/1', 'Read it from the start', numWord(D.pages.length) + ' pages, about ' + readMinutes() + ' minutes, from the product to an open ballot.') +
        entry('#/map', 'Open the map', 'All ' + n(m.records) + ' records, by part, module and type.') +
        entry('#/checks', 'See the checks', n(m.findings) + ' notes: which are style, and which are missing proof.') +
      '</div>' +
      '<p class="sign">A design preview built from the live model. ' + n(m.records) + ' records, ' + n(m.findings) + ' validator notes, and one part — Proof — still blank.</p>' +
      '</div></section>';
  }

  function entry(href, title, text) {
    return '<a class="entry" href="' + href + '"><b>' + esc(title) + '</b><span>' + esc(text) + '</span></a>';
  }

  function numWord(v) { return ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten'][v] || v; }

  function readMinutes() {
    var words = 0;
    D.pages.forEach(function (p) {
      var r = D.records[p.rec];
      if (!r) return;
      var text = (standfirstOf(r) || '') + ' ' + (r.body || []).map(function (b) { return b[1]; }).join(' ') + ' ' +
        Object.keys(r.fields || {}).map(function (k) { var v = r.fields[k]; return typeof v === 'string' ? v : ''; }).join(' ');
      words += text.split(/\s+/).length;
    });
    return Math.max(1, Math.round(words / 220));
  }

  function longDate(iso) {
    var d = new Date(iso + 'T00:00:00');
    return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  /* ---------- today ---------- */
  function row(o) {
    var tag = o.href ? 'a' : 'div';
    var meta = (o.meta || []).filter(Boolean).map(function (x) {
      return '<span' + (o.due && x === o.due ? ' class="due"' : '') + '>' + esc(x) + '</span>';
    }).join('');
    return '<' + tag + (o.href ? ' href="' + o.href + '"' : '') + ' class="row' + (o.href ? '' : ' no-link') + '">' +
      '<span class="mark ' + o.mark + '"></span>' +
      '<span>' +
        '<span class="row-t">' + esc(o.title) + '</span>' +
        '<span class="row-m">' + meta + '</span>' +
        (o.why ? '<span class="row-why">' + esc(o.why) + '</span>' : '') +
      '</span>' +
      '<span class="row-go">' + (o.href ? 'Open' : '') + '</span>' +
      '</' + tag + '>';
  }

  function today() {
    var m = D.metrics;
    var html = '';
    setChrome('today');

    var ballots = D.ballots.map(function (b) {
      return row({
        href: D.records[b.id] ? '#/r/' + encodeURIComponent(b.id) : null,
        mark: 'ballot',
        title: b.title,
        meta: ['Ballot', 'Tier ' + b.tier, b.decider, dueLabel(b.due)],
        due: dueLabel(b.due),
        why: b.question,
      });
    }).join('');

    var decisions = D.decisions;
    var shown = showAllDecisions ? decisions : decisions.slice(0, 6);
    var decisionRows = shown.map(function (d) {
      var rec = D.records[d.rec];
      var why = rec && rec.fields ? (rec.fields.choice || rec.fields.purpose || rec.fields.statement || '') : '';
      return row({
        href: D.records[d.rec] ? '#/r/' + encodeURIComponent(d.rec) : null,
        mark: 'decision',
        title: d.title,
        meta: [typeName(d.kind), 'Tier ' + d.tier, d.n + (d.n === 1 ? ' record' : ' records'), dueLabel(d.due)],
        due: dueLabel(d.due),
        why: why,
      });
    }).join('');

    var sec = D.security.groups
      .slice()
      .sort(function (a, b) { return b.n - a.n; })
      .map(function (g) {
        var meanings = {
          cross: ['Reads and writes across an organizer', 'One organizer reads or writes another organizer\u2019s record. Each place needs a stated rule and a reviewer.'],
          data: ['Personal data, and what erasure reaches', 'Records that store a person\u2019s data and say what deletion does.'],
          calls: ['Calls that carry personal data out', 'A call to another system that takes a person\u2019s data with it.'],
        };
        var m2 = meanings[g.kind] || ['Security review item', 'A place a person has to look at.'];
        return row({
          mark: 'security',
          title: m2[0] + ' \u00b7 ' + n(g.n),
          meta: ['Security review', 'Tier S', 'Reviewer: the founder'],
          why: m2[1],
        });
      }).join('');

    var gaps = gapNotes().slice().sort(function (a, b) { return b.n - a.n; }).slice(0, 4);
    var gapRows = gaps.map(function (f) {
      return row({
        href: '#/checks',
        mark: 'gap',
        title: f.code.replace(/-/g, ' ').replace(/^./, function (c) { return c.toUpperCase(); }),
        meta: [n(f.n) + ' notes', n(f.records) + ' records', 'read by a person'],
        why: f.meaning,
      });
    }).join('');

    html += '<section class="sheet"><div class="sheet-grid"><div>' +
      '<div class="page-kicker"><span class="kicker">Today</span><span class="count">' + esc(longDate(D.generated)) + '</span></div>' +
      '<h1 class="title">What needs you</h1>' +
      '<p class="lede">The model holds ' + n(m.records) + ' records; ' + n(m.settled) + ' are settled and ' + n(m.draft) + ' are still drafted. What waits on one person is small, and it is all here.</p>' +

      group('You vote', D.ballots.length + ' ballots', ballots) +
      group('Decisions due', n(m.decisionsQueued) + ' queued', decisionRows +
        (showAllDecisions || decisions.length <= 6 ? '' : more('Show ' + (decisions.length - 6) + ' more of the ' + n(m.decisionsQueued) + ', as this preview carries them', 'decisions'))) +
      group('Security to acknowledge', n(m.security) + ' items', sec) +
      group('Checks that name a gap', n(sumNotes(gapNotes())) + ' notes', gapRows + '<button class="more" data-go="checks">Open all checks</button>') +
      '</div>' +

      '<aside class="state">' +
        '<h2>The model</h2>' +
        '<div class="spine">' + D.layers.map(function (l) {
          var zero = l.total === 0;
          return '<a class="spine-row" href="#/map" style="text-decoration:none">' +
            '<span class="n">' + esc(l.label) + '</span>' +
            '<span class="v">' + n(l.total) + (zero ? '' : ' · ' + pct(l.settled, l.total) + '%') + '</span>' +
            '<span class="meter' + (zero ? ' zero' : '') + '"><i style="width:' + pct(l.settled, l.total) + '%"></i></span>' +
            '</a>';
        }).join('') + '</div>' +
        '<div class="flag"><b>One part is blank.</b> Proof holds ' + n(m.proofRecords) + ' records because no test run reached the book. Every page still reads, and nothing on it is proved.</div>' +
        '<div class="weight"><h2>Where the weight is</h2>' + D.modules.slice(0, 6).map(function (mod) {
          return '<a class="weight-row" href="#/map/' + encodeURIComponent(mod.id) + '"><span class="wn">' + esc(mod.name) + '</span><span class="wv">' + n(mod.total) + '</span></a>';
        }).join('') + '</div>' +
        '<p class="tiny">Turn to <a class="ref" href="#/read/1">the first page</a> for the prose, or <a class="ref" href="#/map">the map</a> for every record.</p>' +
      '</aside>' +
      '</div></section>';
    return html;
  }

  function group(title, count, rows) {
    return '<section class="group"><div class="group-h"><h2>' + esc(title) + '</h2><span class="c">' + esc(count) + '</span></div>' + rows + '</section>';
  }

  var showAllDecisions = false;

  function more(label, target) {
    return '<button class="more" data-more="' + target + '">' + esc(label) + '</button>';
  }

  /* ---------- field rendering ---------- */
  var FLABEL = {
    summary: 'Summary', boundary: 'Where the line runs', exclusions: 'What it does not do',    addresses: 'Actors it names',
    statement: 'The rule', whyItMatters: 'Why it matters', measure: 'Measured by', target: 'Target', dueBy: 'Due',
    actor: 'Actor', finishedWhen: 'Finished when', steps: 'The steps', betweenSteps: 'Between steps', cites: 'Cites',
    purpose: 'Purpose', whatHappens: 'What happens', exposure: 'Exposure', kind: 'Kind', access: 'Access',
    input: 'Input', raises: 'Raises', serves: 'Serves', reachedThrough: 'Reached through', idempotency: 'Idempotency',
    audit: 'Audit', changes: 'Changes', risk: 'Risk', terms: 'Terms', records: 'Records',
    governs: 'Governs', refusal: 'Refusal', options: 'The options', choice: 'The choice', consequences: 'Consequences',
    reopenedWhen: 'Reopened when', decides: 'Decides', basedOn: 'Based on', dependsOn: 'Depends on',
    authority: 'Authority', applies: 'Applies', appliesBecause: 'Applies because', questions: 'The question',
    effect: 'Effect', requires: 'Requires', exposure2: 'Exposure', requiresProof: 'Requires proof'
  };

  var STAND = { product: 'summary', module: 'summary', constraint: 'summary', goal: 'statement', journey: 'finishedWhen', 'use-case': 'purpose', rule: 'statement', decision: 'choice' };

  var ORDER = {
    product: ['summary', 'boundary', 'exclusions', 'addresses'],
    goal: ['statement', 'whyItMatters', 'measure', 'target', 'dueBy'],
    journey: ['actor', 'finishedWhen', 'steps', 'betweenSteps', 'cites'],
    'use-case': ['purpose', 'whatHappens', 'exposure', 'kind', 'access', 'input', 'raises', 'serves', 'reachedThrough', 'idempotency', 'audit', 'risk'],
    rule: ['statement', 'governs', 'refusal', 'kind'],
    decision: ['options', 'choice', 'consequences', 'reopenedWhen', 'decides', 'basedOn'],
    module: ['summary', 'dependsOn'],
    constraint: ['summary', 'authority', 'kind', 'applies', 'appliesBecause', 'questions'],
    aggregate: ['summary', 'states', 'changes'],
    screen: ['summary', 'route', 'shows']
  };

  var PULL = { statement: 1, choice: 1, finishedWhen: 1, purpose: 1 };

  function isRefId(v) { return typeof v === 'string' && /^[a-z][a-z-]*\/[a-z0-9]/.test(v) && v.indexOf(' ') < 0; }

  function refList(v) {
    var arr = Array.isArray(v) ? v : [v];
    return arr.map(function (x) { return typeof x === 'string' ? ref(x) : esc(JSON.stringify(x)); }).join(' · ');
  }

  function table(rows, caption) {
    if (!rows || !rows.length) return '';
    var cols = Object.keys(rows[0]).filter(function (k) { return !['approvedHash'].includes(k); });
    var cap = caption ? '<caption>' + esc(caption) + '</caption>' : '';
    return '<table class="table">' + cap +
      '<thead><tr>' + cols.map(function (c) { return '<th>' + esc(FLABEL[c] || c) + '</th>'; }).join('') + '</tr></thead>' +
      '<tbody>' + rows.map(function (r) {
        return '<tr>' + cols.map(function (c) {
          var v = r[c];
          if (Array.isArray(v)) v = v.join('; ');
          if (v && typeof v === 'object') v = Object.keys(v).map(function (k) { return k + ': ' + v[k]; }).join('; ');
          if (isRefId(v)) return '<td>' + ref(v) + '</td>';
          return '<td>' + esc(v == null ? '' : v) + '</td>';
        }).join('') + '</tr>';
      }).join('') + '</tbody></table>';
  }

  function renderField(key, value, rec) {
    var label = FLABEL[key] || key.replace(/([A-Z])/g, ' $1').toLowerCase();
    if (value == null || value === '' || (Array.isArray(value) && !value.length)) return '';
    if (!PROSE[key]) value = value; // lists and tables carry their own label
    var head = PROSE[key] ? '' : '<h3>' + esc(label) + '</h3>';

    if (key === 'exposure' && typeof value === 'string') {
      return '<p class="callout"><span class="label">Exposure</span>' + esc(value) + '</p>';
    }
    if (key === 'steps' && Array.isArray(value)) {
      return head + '<ol class="steps">' + value.map(function (s) {
        return '<li>' + (s.actor ? '<span class="actor">' + esc(titleOf(s.actor)) + '</span>' : '') + esc(s.statement || '') +
          (s.failure ? '<span class="fail">If not: ' + esc(s.failure) + '</span>' : '') + '</li>';
      }).join('') + '</ol>';
    }
    if (key === 'options' && Array.isArray(value)) {
      var chosen = (rec.fields && rec.fields.choice) || '';
      return '<table class="table"><caption>The options</caption><thead><tr><th>Option</th><th>For</th><th>Against</th></tr></thead><tbody>' +
        value.map(function (o) {
          var win = o.name === chosen;
          return '<tr class="' + (win ? 'chosen' : '') + '"><td>' + esc(o.name) + (win ? '<span class="choice-mark">chosen</span>' : '') + '</td><td>' + esc(o.pros || '') + '</td><td>' + esc(o.cons || '') + '</td></tr>';
        }).join('') + '</tbody></table>';
    }
    if (key === 'input' && Array.isArray(value)) {
      return '<table class="table"><caption>The input</caption><thead><tr><th>Name</th><th>Meaning</th><th>Kind</th><th>Rule</th></tr></thead><tbody>' +
        value.map(function (i) {
          return '<tr><td class="mono">' + esc(i.name) + '</td><td>' + esc(i.meaning || '') + '</td><td>' + esc([i.type, i.classification].filter(Boolean).join(' · ')) + '</td><td>' + esc((i.rules || []).join(' ')) + '</td></tr>';
        }).join('') + '</tbody></table>';
    }
    if ((key === 'questions' || key === 'effects') && Array.isArray(value)) {
      return value.map(function (q) {
        if (typeof q === 'string') return '<p>' + esc(q) + '</p>';
        return '<p>' + esc(q.question || q.statement || q.text || '') + '</p>';
      }).join('');
    }
    if (Array.isArray(value)) {
      if (value.every(isRefId)) return head + '<p>' + refList(value) + '</p>';
      if (value.every(function (v) { return typeof v === 'string'; })) {
        return head + '<ul>' + value.map(function (v) { return '<li>' + esc(v) + '</li>'; }).join('') + '</ul>';
      }
      if (value.every(function (v) { return v && typeof v === 'object'; })) return table(value, label);
      return head + '<p>' + esc(value.join(', ')) + '</p>';
    }
    if (typeof value === 'object') {
      return head + '<p>' + Object.keys(value).map(function (k) { return esc(k + ': ' + value[k]); }).join(' · ') + '</p>';
    }
    if (PULL[key]) return '<p class="pull">' + esc(value) + '</p>';
    if (isRefId(value)) return head + '<p>' + ref(value) + '</p>';
    return head + '<p>' + esc(value) + '</p>';
  }

  var FACT = { measure: 1, target: 1, dueBy: 1, kind: 1, risk: 1, idempotency: 1, audit: 1, applies: 1, requires: 1, requiresProof: 1, actor: 1 };
  var PROSE = { summary: 1, statement: 1, choice: 1, whyItMatters: 1, boundary: 1, whatHappens: 1, purpose: 1, finishedWhen: 1, consequences: 1, reopenedWhen: 1, exposure: 1, appliesBecause: 1 };

  function factValue(v) {
    if (typeof v === 'boolean') return v ? 'yes' : 'no';
    if (/^\d{4}-\d{2}-\d{2}$/.test(String(v))) return longDate(v);
    if (isRefId(v)) return ref(v);
    return esc(v);
  }

  function bodyOf(rec) {
    var type = rec.type;
    var fields = rec.fields || {};
    var order = (ORDER[type] || []).slice();
    var hasBallot = (D.ballots || []).some(function (b) { return b.id === rec.id; });
    Object.keys(fields).forEach(function (k) {
      if (order.indexOf(k) < 0 && !['approvedBy', 'approvedOn', 'summary'].includes(k)) order.push(k);
    });
    var standKey = STAND[type];
    var html = '';
    var facts = [];
    order.forEach(function (k) {
      if (k === standKey) return;
      if (k === 'questions' && hasBallot) return;
      if (['approvedBy', 'approvedOn'].indexOf(k) > -1) return;
      var v = fields[k];
      if (v == null || v === '' || (Array.isArray(v) && !v.length)) return;
      if (FACT[k] && typeof v !== 'object') { facts.push([FLABEL[k] || k, v]); return; }
      html += renderField(k, v, rec);
    });
    if (facts.length) {
      html = '<p class="facts">' + facts.map(function (f) {
        return '<span class="k">' + esc(f[0]) + '</span> <b>' + factValue(f[1]) + '</b>';
      }).join('') + '</p>' + html;
    }
    if (rec.body && rec.body.length) {
      rec.body.forEach(function (b) {
        if (b[0] === 'h') html += '<h3>' + esc(b[1]) + '</h3>';
        else if (b[0] === 'li') html += '<p>· ' + esc(b[1]) + '</p>';
        else html += '<p>' + esc(b[1]) + '</p>';
      });
    }
    return html;
  }

  function standfirstOf(rec) {
    var key = STAND[rec.type];
    var v = rec.fields && rec.fields[key];
    if (Array.isArray(v)) v = v[0];
    if (typeof v === 'string') return v;
    return rec.fields && rec.fields.summary ? rec.fields.summary : '';
  }

  /* ---------- graph ---------- */
  var VERB = {
    'use-case.serves': 'Serves these journeys', 'use-case.fulfills': 'Fulfils these requirements', 'rule.governs': 'Governs',
    'aggregate.records.governs': 'Governed by', 'aggregate.transitions.by': 'Changed by',
    'goal.served-by': 'Served by', 'journey.serves': 'Serves the goal', 'module.depends-on': 'Depends on',
    'decision.decides': 'Decides', 'constraint.questions': 'Asked about', 'use-case.raises': 'Raises',
    'use-case.reached-through': 'Reached through', 'rule.refusal': 'Refused with', 'requirement.fulfilled-by': 'Fulfilled by'
  };

  var PLURAL = function (t) { var l = lexOf(t); return l && l.plural ? l.plural : typeName(t) + 's'; };

  function graphOf(rec) {
    var dirs = [['It points to', rec.out || []], ['Points here', rec.in || []]];
    var html = '';
    dirs.forEach(function (d) {
      var groups = {};
      d[1].forEach(function (e) {
        var t = typeOf(e[0]);
        if (t === 'code' || t === 'test') return;
        groups[t] = groups[t] || [];
        if (groups[t].indexOf(e[0]) < 0) groups[t].push(e[0]);
      });
      var types = Object.keys(groups);
      if (!types.length) return;
      html += '<p class="gl"><span class="label">' + esc(d[0]) + '</span></p>';
      types.forEach(function (t) {
        html += '<p class="gl gsub"><span class="label">' + esc(PLURAL(t)) + ' · ' + groups[t].length + '</span> ' +
          groups[t].slice(0, 8).map(function (id) { return ref(id); }).join(' · ') + '</p>';
      });
    });
    if (!html) return '';
    return '<h3>Where it sits</h3><div class="graph">' + html + '</div>';
  }

  /* ---------- record page ---------- */
  function notesFor(rec) {
    var notes = [];
    notes.push(note('Status', esc(rec.status || '') + (rec.effective && rec.effective !== rec.status ? ' (reads as ' + esc(rec.effective) + ')' : '')));
    if (rec.decider) notes.push(note('Decided by', esc(rec.decider) + (rec.decideBy ? ' · ' + esc(dueLabel(rec.decideBy)) : '')));
    if (rec.owner) notes.push(note('Owner', esc(rec.owner)));
    if (rec.fields && rec.fields.risk) notes.push(note('Risk', esc(rec.fields.risk)));
    if (rec.src) notes.push(note('Written in', '<span class="mono">' + esc(rec.src[0]) + ':' + esc(rec.src[1]) + '</span>'));
    var gaps = rec.finds && (rec.finds.S + rec.finds.G);
    if (rec.finds && (rec.finds.A || gaps)) {
      notes.push(note('The validator', (gaps ? '<b style="color:var(--bad)">' + gaps + ' security or governance notes</b>. ' : '') +
        (rec.finds.A ? n(rec.finds.A) + ' style notes on wording, folded away.' : '') +
        ' <a class="ref" href="#/checks">See checks</a>'));
    }
    if (rec.impl && rec.impl.length) notes.push(note('Built in', n(rec.impl.length) + ' places' + (rec.tests ? ', proved by ' + n(rec.tests) + ' tests' : ', no test yet')));
    if ((rec.type === 'use-case' || rec.type === 'screen') && rec.expect && rec.expect.proof && !rec.tests) notes.push(note('Proof', '<span style="color:var(--bad)">Expected, and missing</span>'));
    return notes.join('');
  }

  function note(k, v) { return '<div class="note"><b>' + esc(k) + '</b><div class="v">' + v + '</div></div>'; }

  function ballotBlock(rec) {
    var b = (D.ballots || []).filter(function (x) { return x.id === rec.id; })[0];
    if (!b) return '';
    return '<div class="ballot" data-ballot="' + esc(rec.id) + '">' +
      '<span class="label">Open ballot · ' + esc(b.decider || 'the founder') + ' · ' + esc(dueLabel(b.due)) + '</span>' +
      '<p class="bq">' + esc(b.question) + '</p>' +
      '<p class="br"><b>The recommendation.</b> ' + esc(b.recommendation) + '</p>' +
      '<div class="bact"><button class="btn2" data-vote="yes">Record my vote: yes</button><button class="btn2" data-vote="no">Ask for another round</button><span class="bnote">This preview records nothing.</span></div>' +
      '</div>';
  }

  function prettyId(id) {
    var tail = String(id).split('/').pop().split('.').pop().replace(/-/g, ' ');
    return tail.charAt(0).toUpperCase() + tail.slice(1);
  }

  function recordPage(id) {
    var rec = D.records[id];
    setChrome('record');
    if (!rec) {
      var st = IDX.ids.indexOf(id);
      var label = st > -1 ? IDX.titles[st] : (D.labels[id] && D.labels[id].t) || prettyId(id);
      return '<section class="stub"><span class="kicker">' + esc(typeName(typeOf(id))) + '</span>' +
        '<h1 class="title">' + esc(label) + '</h1>' +
        '<p>This page is part of the real book. The preview carries ' + n(Object.keys(D.records).length) + ' records in full; this one is listed, indexed and searchable, and opens here in the book itself.</p>' +
        '<p class="mono" style="font-size:12px">' + esc(id) + '</p>' +
        '<p><a class="ref" href="#/map">Back to the map</a></p></section>';
    }
    var pageNo = 0;
    D.pages.forEach(function (p, i) { if (p.rec === id) pageNo = i + 1; });
    var head = '<div class="page-head"><div class="kicker"><span>' + esc(typeName(rec.type)) + '</span>' +
      (typeQuestion(rec.type) ? '<span class="dot"></span><span>' + esc(typeQuestion(rec.type)) + '</span>' : '') + '</div>' +
      '<h1 class="page-title">' + esc(rec.title) + '</h1>' +
      '<p class="stand">' + esc(standfirstOf(rec)) + '</p></div>';
    var body = '<div class="prose">' + bodyOf(rec) + ballotBlock(rec) +
      (pageNo ? '<p class="tiny">This is page ' + pageNo + ' of the book. <a class="ref" href="#/read/' + pageNo + '">Read it in order</a>.</p>' : '') +
      graphOf(rec) +
      '</div>' +
      '<div class="rawbar"><button class="btn2" data-raw="' + esc(rec.id) + '">Show the record</button><span class="tiny" style="margin:0">The same page as data: fields, provenance and links.</span></div>' +
      '<pre class="raw" id="raw" hidden></pre>';
    return '<article class="read"><div class="page">' + head + body + '</div>' +
      '<aside class="notes"><div class="label">This page</div>' + notesFor(rec) + '</aside>' +
      '</article>';
  }

  /* ---------- read ---------- */
  function read(page) {
    var total = D.pages.length;
    var i = Math.max(1, Math.min(total, page || 1));
    var p = D.pages[i - 1];
    var rec = D.records[p.rec];
    setChrome('read', i, total);
    if (!rec) return recordPage(p.rec);

    var head = '<div class="page-head"><div class="kicker"><span>Part ' + roman(partIndex(p.part)) + ' · ' + esc(p.part) + '</span><span class="dot"></span><span>Page ' + i + ' of ' + total + '</span></div>' +
      '<h1 class="page-title">' + esc(p.title) + '</h1>' +
      '<p class="stand">' + esc(standfirstOf(rec)) + '</p></div>';

    var body = '<div class="prose">' + bodyOf(rec) + ballotBlock(rec) + '</div>';

    var prev = D.pages[i - 2];
    var next = D.pages[i];
    var pager = '<nav class="pager">' +
      (prev ? '<a href="#/read/' + (i - 1) + '"><span class="k">Previous</span><span class="t">' + esc(prev.title) + '</span></a>' : '<span class="empty"></span>') +
      (next ? '<a class="next" href="#/read/' + (i + 1) + '"><span class="k">Next</span><span class="t">' + esc(next.title) + '</span></a>' : '<a class="next" href="#/map"><span class="k">The end</span><span class="t">Open the map</span></a>') +
      '</nav>';

    var side = '<div class="label">' + esc(typeName(rec.type)) + ' · page ' + i + '</div>' + notesFor(rec) + tocBlock(i) +
      '<div class="note"><b>Turn the page</b><div class="v">j and k, or the arrow keys.</div></div>';

    return '<article class="read"><div class="page">' + head + body + pager + '</div>' +
      '<aside class="notes">' + side + '</aside></article>';
  }

  function tocBlock(current) {
    var html = '';
    var lastPart = '';
    D.pages.forEach(function (p, idx) {
      if (p.part !== lastPart) {
        html += '<span class="tpart">' + esc(p.part) + '</span>';
        lastPart = p.part;
      }
      html += '<a class="tpage' + (idx + 1 === current ? ' cur' : '') + '" href="#/read/' + (idx + 1) + '"><span class="tn">' + (idx + 1) + '</span>' + esc(p.title) + '</a>';
    });
    return '<div class="note"><b>Contents</b><div class="toclist">' + html + '</div></div>';
  }

  function partIndex(part) {
    var seen = [];
    D.pages.forEach(function (p) { if (seen.indexOf(p.part) < 0) seen.push(p.part); });
    return seen.indexOf(part) + 1;
  }

  function roman(n2) { return ['I', 'II', 'III', 'IV', 'V', 'VI'][n2 - 1] || n2; }

  /* ---------- map ---------- */
  function map() {
    setChrome('map');
    var m = D.metrics;
    var lens = localStorage.getItem('book.lens') || 'founder';
    var html = '<section class="mapwrap">' +
      '<div class="map-head"><div>' +
        '<div class="page-kicker"><span class="kicker">The map</span></div>' +
        '<h1 class="title">Everything in the model</h1>' +
        '<p class="lede">Six parts hold ' + n(m.records) + ' records. A part is settled when its records are approved or reviewed; one part is blank.</p>' +
      '</div><div class="lens" role="group" aria-label="Lens">' +
        '<button data-lens="founder" aria-pressed="' + (lens === 'founder') + '">Founder</button>' +
        '<button data-lens="engineer" aria-pressed="' + (lens === 'engineer') + '">Engineer</button>' +
      '</div></div>' +

      '<table class="grid"><thead><tr><th>Part</th><th class="r">Records</th><th class="hide-s">Settled</th><th class="r">Draft</th><th class="hide-s">Holds</th></tr></thead><tbody>' +
      D.layers.map(function (l) {
        var draft = l.total - l.settled;
        return '<tr><td class="part">' + esc(l.label) + '</td>' +
          '<td class="c r">' + n(l.total) + '</td>' +
          '<td class="hide-s">' + meter(l.settled, l.total, l.total === 0) + '<span class="pct">' + (l.total ? pct(l.settled, l.total) + '%' : '—') + '</span></td>' +
          '<td class="m r">' + (draft ? n(draft) : '—') + '</td>' +
          '<td class="hide-s">' + typeCountsHtml(l.types, lens === 'founder' ? 4 : 20) + '</td></tr>';
      }).join('') + '</tbody></table>' +

      '<h2 class="title" style="font-size:26px;margin-top:52px">Behavior, by module</h2>' +
      '<p class="lede" style="font-size:15.5px">Twenty-six modules carry most of the model. The order is by weight, so the modules that need the most reading come first.</p>' +
      '<table class="grid"><thead><tr><th>Module</th><th class="r">Records</th><th class="hide-s">Settled</th><th class="r">Use cases</th><th class="r hide-x">Rules</th><th class="r hide-x">Checks</th></tr></thead><tbody>' +
      D.modules.map(function (mod) {
        var uc = (mod.types.filter(function (t) { return t[0] === 'use-case'; })[0] || [0, 0])[1];
        var rules = (mod.types.filter(function (t) { return t[0] === 'rule'; })[0] || [0, 0])[1];
        var checks = lens === 'founder' ? (mod.findingsGap || 0) : (mod.findings || 0);
        return '<tr class="mod" data-mod="' + esc(mod.id) + '">' +
          '<td class="name">' + esc(mod.name) + '</td>' +
          '<td class="c r">' + n(mod.total) + '</td>' +
          '<td class="hide-s">' + meter(mod.settled, mod.total) + '<span class="pct">' + pct(mod.settled, mod.total) + '%</span></td>' +
          '<td class="c r">' + n(uc) + '</td>' +
          '<td class="c r hide-x">' + n(rules) + '</td>' +
          '<td class="c r hide-x">' + (checks ? n(checks) : '—') + '</td></tr>';
      }).join('') + '</tbody></table>' +
      '<p class="legend">' + (lens === 'founder'
        ? 'The founder lens counts only the checks that name a gap — missing proof, missing refusal text, a promise with no code. Style notes on wording are hidden; they live under Checks.'
        : 'The engineer lens counts every note the validator writes, including the ' + n(m.styleFindings) + ' style notes on wording.') +
      ' Only the Accounts module opens in full in this preview.</p>' +
      '</section>';
    return html;
  }

  function modulePage(id) {
    setChrome('module');
    var mod = D.modules.filter(function (x) { return x.id === id; })[0];
    if (!mod) return map();
    var rows = [];
    IDX.ids.forEach(function (rid, i) {
      if (typeOf(rid) === 'module' || !rid.split('/')[1] || rid.split('/')[1].split('.')[0] !== id) return;
      if (typeOf(rid) === 'test' || typeOf(rid) === 'finding') return;
      rows.push({ id: rid, title: IDX.titles[i], st: IDX.st[i], type: typeOf(rid) });
    });
    var byType = {};
    rows.forEach(function (r) { (byType[r.type] = byType[r.type] || []).push(r); });
    var typeOrder = mod.types.map(function (t) { return t[0]; });
    return '<section class="modwrap">' +
      '<div class="page-kicker"><span class="kicker">Behavior · module</span></div>' +
      '<h1 class="title">' + esc(mod.name) + '</h1>' +
      '<p class="lede">' + esc((D.records['module/' + id] && D.records['module/' + id].fields.summary) || 'A module of the behavior part.') + '</p>' +
      '<div class="triage" style="margin-top:28px">' +
        '<div><b>' + n(mod.total) + '</b><span>records</span></div>' +
        '<div><b>' + pct(mod.settled, mod.total) + '%</b><span>settled</span></div>' +
        '<div><b>' + n(mod.findingsGap || 0) + '</b><span>checks that name a gap</span></div>' +
        '<div><b>' + n(mod.unimplemented || 0) + '</b><span>effects promised, with no code</span></div>' +
      '</div>' +
      '<div class="typecols">' + typeOrder.map(function (t) {
        var list = byType[t] || [];
        if (!list.length) return '';
        return '<div class="tcol"><h3>' + esc(typeName(t)) + '<span class="c">' + list.length + '</span></h3><ul>' +
          list.slice(0, 60).map(function (r) {
            var inner = esc(r.title);
            if (D.records[r.id]) inner = '<a href="#/r/' + encodeURIComponent(r.id) + '">' + inner + '</a>';
            return '<li><span class="st ' + r.st + '"></span><span>' + inner + '</span></li>';
          }).join('') + '</ul></div>';
      }).join('') + '</div>' +
      '<p class="legend"><a class="ref" href="#/map">Back to the map</a> · Only records this preview carries open; the rest are named here as they are in the book.</p>' +
      '</section>';
  }

  /* ---------- checks ---------- */
  // Notes that name something missing, whatever tier the validator stamped on them.
  var NAMES_A_GAP = {
    'use-case-unverified': 1, 'result-scenario-missing': 1, 'refusal-sentence-missing': 1, 'use-case-unreached': 1,
    'requirement-measure-missing': 1, 'effect-unimplemented': 1, 'step-screen-missing': 1, 'use-case-act-missing': 1,
    'requirement-fulfillment-missing': 1, 'proves-unreached': 1, 'proves-duplicate': 1, 'use-case-method-drift': 1,
    'route-invalid': 1, 'effect-undeclared': 1, 'unreachable-screen': 1
  };
  function namesAGap(f) { return f.tier !== 'A' || !!NAMES_A_GAP[f.code]; }
  function gapNotes() { return D.findings.filter(namesAGap); }
  function wordingNotes() { return D.findings.filter(function (f) { return !namesAGap(f); }); }
  function sumNotes(list) { return list.reduce(function (n2, f) { return n2 + f.n; }, 0); }

  function checks() {
    setChrome('checks');
    var m = D.metrics;
    var lens = localStorage.getItem('book.lens') || 'founder';
    var gaps = gapNotes();
    var style = wordingNotes();
    var rows = lens === 'founder' ? gaps : D.findings;
    var hiddenCount = lens === 'founder' ? sumNotes(style) : 0;
    var unprovedUc = (D.findings.filter(function (f) { return f.code === 'use-case-unverified'; })[0] || { n: 0 }).n;

    return '<section class="checks">' +
      '<div class="page-kicker"><span class="kicker">Checks</span></div>' +
      '<h1 class="title">What the validator says</h1>' +
      '<p class="lede">' + n(m.findings) + ' notes on ' + n(m.records) + ' records. They are not one kind of thing, and reading them as one list is how a founder drowns. ' + n(unprovedUc) + ' use cases a buyer or organizer reaches have no end-to-end proof, and the Proof part is empty.</p>' +

      '<div class="triage">' +
        '<div><b>' + n(m.findings) + '</b><span>notes in all, on ' + n(m.records) + ' records.</span></div>' +
        '<div><b>' + n(sumNotes(style)) + '</b><span>about wording. Hidden by default; a tool can fix these.</span></div>' +
        '<div><b>' + n(sumNotes(gaps)) + '</b><span>that name something missing. A person reads these.</span></div>' +
        '<div><b>' + n(m.proofRecords) + '</b><span>records in the Proof part. It is empty.</span></div>' +
      '</div>' +

      '<div class="map-head" style="margin-top:38px"><div><h2 class="title" style="font-size:24px">By note</h2></div>' +
        '<div class="lens" role="group" aria-label="Lens">' +
          '<button data-lens="founder" aria-pressed="' + (lens === 'founder') + '">What needs a person</button>' +
          '<button data-lens="engineer" aria-pressed="' + (lens === 'engineer') + '">Everything</button>' +
        '</div></div>' +

      '<table class="findings"><thead><tr><th>Note</th><th class="r">Count</th><th>What it means</th></tr></thead><tbody>' +
      rows.map(function (f) {
        return '<tr><td><div class="code">' + esc(f.code) + '</div><span class="tier tier-' + esc(f.tier) + '">' + esc(f.tier) + '</span></td>' +
          '<td class="n">' + n(f.n) + '</td>' +
          '<td><div class="means">' + esc(f.meaning) + '</div><div class="sample">' + esc(f.sample || '') + '</div></td></tr>';
      }).join('') + '</tbody></table>' +
      (lens === 'founder' ? '<p class="hidden-notes">' + n(hiddenCount) + ' notes hidden, all of them about wording: a sentence that starts without an article, a field whose text misses a pattern, a source file that drifted from its format. No decision depends on them. <button class="more" data-lens="engineer">Show every note</button></p>' : '') +
      '<p class="legend">The tier on each row is the validator\u2019s own label. Whether a note is a gap or a wording fix is decided by what it points at, which is why a few tier A notes sit here: “a use case has no end-to-end proof” is not a style matter.</p>' +
      '</section>';
  }

  /* ---------- design notes ---------- */
  function notes() {
    setChrome('notes');
    return '<section class="notespg">' +
      '<div class="page-kicker"><span class="kicker">Design notes</span></div>' +
      '<h1 class="title">Why this shape</h1>' +
      '<p class="lede">A note on the preview, so you can judge the argument and not only the pixels.</p>' +

      '<h2>What I saw in the current book</h2>' +
      '<p>It is a careful tool, and it is built for the wrong reader. The first screen is a 21,919-node outline, a detail pane and a third rail of connections. The outline puts validator findings — <em>“a data CreatedAt meaning that does not start with A, An or The”</em> — between the rules and the use cases of an aggregate. A founder scrolling that is reading a compiler log with a table of contents.</p>' +
      '<p>The numbers hide the shape of the problem: 28,134 of 28,801 notes are about wording; 1,579 records are drafts; 3 ballots are open; and the Proof part is empty. In the current book those four facts sit in four different places, or nowhere. The information is all there. The order is not.</p>' +

      '<h2>What I changed, and the one idea behind it</h2>' +
      '<p><strong>Rank the model by what a person must do, not by how the model is stored.</strong> Everything else follows from that.</p>' +
      '<ul>' +
      '<li><strong>A cover, not a tree.</strong> The book opens on a page that says what this is, how big it is, and where to start. Nobody is asked to read 21,919 records in an outline; they are asked to turn a page.</li>' +
      '<li><strong>Today is a worklist, ranked.</strong> Ballots you vote on, decisions due, security to acknowledge, checks that name a gap. Each row is one sentence of consequence. Findings are never a branch of the contents.</li>' +
      '<li><strong>Read is linear and paginated.</strong> Nine pages from the product to an open ballot, on a 34rem measure at 18px, with notes in the margin. This is the mode that earns the name “book”, and the only one that answers “what does this product promise?” for someone who is not a developer.</li>' +
      '<li><strong>The map holds the volume.</strong> Six parts, then 26 modules, with counts by type and a settled meter. Collapsing 19,949 behavior records into a 26-row table is the actual fix for “overwhelming”: the numbers are still there, one glance away.</li>' +
      '<li><strong>One lens, stated.</strong> Founder hides style notes and counts only gaps; engineer shows everything. Same data, two readers, no ambiguity about which is speaking.</li>' +
      '<li><strong>A record is prose first, data second.</strong> “Show the record” unfolds the raw fields on the same page, so the developer never needs a different tool and the founder never sees JSON unless they ask.</li>' +
      '</ul>' +

      '<h2>What is real here</h2>' +
      '<p>Every number, title and paragraph comes from the generated NightHall model: 21,919 records, the real ballots, the real decision batches, the real validator notes. Search covers all 21,919 titles; ' + n(Object.keys(D.records).length) + ' records are carried in full. Nothing is lorem ipsum, and nothing is a screenshot.</p>' +

      '<h2>What I would change in the generator</h2>' +
      '<ul>' +
      '<li>Emit two indexes: a worklist (ballots, decisions, security, gap checks, ordered by due date) and a contents (parts, modules, types). Today both have to be derived from the outline.</li>' +
      '<li>Keep findings out of the outline. They are a view over records, never children of them.</li>' +
      '<li>Classify each note by what a person must do — a gap, a decision, a wording fix — instead of leaving that judgement to its check tier. “A use case has no proof” and “a sentence starts without an article” are not the same message, whatever tier carries them.</li>' +
      '<li>Cut every record to a standfirst and a body of ordered blocks, so any renderer — page, email, PDF — reads the same shape.</li>' +
      '<li>Say which part is blank in the manifest, so the book can print the fact instead of a reader noticing it.</li>' +
      '<li>Add a “changed since you last read” list. A founder reads what moved, not the whole book again.</li>' +
      '</ul>' +

      '<h2>What I did not do</h2>' +
      '<p>No charts. No sparklines, no radial progress, no dashboard of twelve cards: a founder does not need a gauge for 21,919 records, they need the three things that are waiting. No infinite outline. No second navigation rail. The keyboard is the shortcut (<span class="mono">/</span> to find, <span class="mono">j</span> and <span class="mono">k</span> to turn pages), not the interface.</p>' +

      '<p class="tiny" style="margin-top:34px"><a class="ref" href="#/">Back to the cover</a></p>' +
      '</section>';
  }

  /* ---------- palette ---------- */
  var palette = { open: false, index: 0, results: [] };

  function openPalette() {
    var el = $('#overlay');
    el.innerHTML = '<div class="scrim" data-close="1"><div class="plate" role="dialog" aria-modal="true" aria-label="Find a record">' +
      '<div class="field"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>' +
      '<input id="pq" type="search" autocomplete="off" spellcheck="false" placeholder="Find a record by title, or type “ballot”" aria-label="Find a record">' +
      '<span class="esc">esc</span></div><div class="results" id="presults"></div></div></div>';
    palette.open = true;
    palette.index = 0;
    $('#pq').addEventListener('input', runSearch);
    $('#pq').focus();
    runSearch();
  }

  function closePalette() {
    palette.open = false;
    $('#overlay').innerHTML = '';
  }

  function score(title, q) {
    var t = title.toLowerCase();
    if (t === q) return 0;
    if (t.indexOf(q) === 0) return 1;
    var wi = t.indexOf(' ' + q);
    if (wi > -1) return 2;
    var at = t.indexOf(q);
    if (at > -1) return 3 + Math.min(4, at / 40);
    return -1;
  }

  function runSearch() {
    var input = $('#pq');
    if (!input) return;
    var q = input.value.trim().toLowerCase();
    var out = [];
    if (q.length < 1) {
      var seeds = ['product/nighthall', 'goal/churn-under-3-percent', 'journey/use-a-buyer-account', 'module/accounts', 'use-case/accounts.change-user-account-profile', 'decision/0086-buyer-locale-fallback', 'constraint/platform-hosting-duties'];
      out = seeds.map(function (id) {
        var at = IDX.ids.indexOf(id);
        return { id: id, title: at > -1 ? IDX.titles[at] : titleOf(id), st: at > -1 ? IDX.st[at] : 'r' };
      });
    } else {
      var hits = [];
      for (var i = 0; i < IDX.titles.length; i++) {
        var s = score(IDX.titles[i], q);
        if (s < 0) continue;
        hits.push([s, i]);
      }
      hits.sort(function (a, b) { return a[0] - b[0] || a[1] - b[1]; });
      out = hits.slice(0, 40).map(function (h) {
        return { id: IDX.ids[h[1]], title: IDX.titles[h[1]], st: IDX.st[h[1]] };
      });
    }
    palette.results = out;
    palette.index = 0;
    renderResults(q);
  }

  function renderResults(q) {
    var box = $('#presults');
    if (!box) return;
    if (!palette.results.length) {
      box.innerHTML = '<div class="hint">Nothing in the model matches “' + esc(q) + '”.</div>';
      return;
    }
    box.innerHTML = (q ? '<div class="hint">' + n(palette.results.length) + ' of ' + n(IDX.ids.length) + ' records, best first</div>' : '<div class="hint">Start here</div>') +
      palette.results.map(function (r, i) {
        return '<div class="item" data-i="' + i + '" data-id="' + esc(r.id) + '" role="option" aria-selected="' + (i === palette.index) + '">' +
          '<span class="dt ' + r.st + '"></span>' +
          '<span><span class="nm">' + esc(r.title) + '</span><br><span class="sr2">' + esc(r.id) + '</span></span>' +
          '<span class="ty">' + esc(typeName(typeOf(r.id))) + '</span></div>';
      }).join('');
  }

  function movePalette(delta) {
    if (!palette.results.length) return;
    palette.index = Math.max(0, Math.min(palette.results.length - 1, palette.index + delta));
    Array.prototype.forEach.call(document.querySelectorAll('.item'), function (el, i) {
      el.setAttribute('aria-selected', i === palette.index);
      if (i === palette.index) el.scrollIntoView({ block: 'nearest' });
    });
  }

  function choosePalette() {
    var r = palette.results[palette.index];
    if (!r) return;
    closePalette();
    location.hash = '#/r/' + encodeURIComponent(r.id);
  }

  /* ---------- routing ---------- */
  function route() {
    var raw = location.hash.replace(/^#\/?/, '');
    var parts = raw.split('/');
    var head = parts[0] || '';
    var main = $('#main');
    var html;

    if (head === '' || head === 'cover') html = cover();
    else if (head === 'today') html = today();
    else if (head === 'read') html = read(Number(parts[1] || 1));
    else if (head === 'map') html = parts[1] ? modulePage(decodeURIComponent(parts[1])) : map();
    else if (head === 'checks') html = checks();
    else if (head === 'notes') html = notes();
    else if (head === 'r') html = recordPage(decodeURIComponent(parts.slice(1).join('/')));
    else html = cover();

    main.innerHTML = html;
    foot();
    var named = { '': 'The cover', today: 'Today', map: 'The map', checks: 'Checks', notes: 'Design notes' };
    document.title = (head === 'read' ? 'Page ' + (parts[1] || 1) : named[head] || 'The book') + ' · ' + D.project.title;
    var focusMain = document.activeElement === document.body;
    if (focusMain) main.focus({ preventScroll: true });
  }

  function foot() {
    $('#foot').innerHTML = '<span><b>' + esc(D.project.title) + '</b> · ' + n(D.metrics.records) + ' records · ' + esc(D.edition) + ' · ' + esc(longDate(D.generated)) + '</span>' +
      '<span class="right"><a href="#/notes">Design notes</a><span><span class="mono">/</span> find · <span class="mono">j k</span> turn pages</span></span>';
  }

  /* ---------- events ---------- */
  document.addEventListener('click', function (e) {
    var t = e.target;

    var close = t.closest('[data-close]');
    if (close && (t === close || t.closest('.plate') === null)) { closePalette(); return; }

    var item = t.closest('.item');
    if (item) { palette.index = Number(item.getAttribute('data-i')); choosePalette(); return; }

    var lens = t.closest('[data-lens]');
    if (lens) {
      localStorage.setItem('book.lens', lens.getAttribute('data-lens'));
      route();
      return;
    }

    var mod = t.closest('[data-mod]');
    if (mod && !t.closest('a')) { location.hash = '#/map/' + encodeURIComponent(mod.getAttribute('data-mod')); return; }

    var raw = t.closest('[data-raw]');
    if (raw) {
      var pre = $('#raw');
      var id = raw.getAttribute('data-raw');
      if (pre) {
        if (pre.hidden) {
          var rec = D.records[id];
          pre.textContent = JSON.stringify({ id: rec.id, type: rec.type, status: rec.status, source: rec.src, fields: rec.fields, built: rec.impl, tests: rec.tests, links: { out: rec.out, in: rec.in } }, null, 2);
          pre.hidden = false;
          raw.textContent = 'Hide the record';
        } else {
          pre.hidden = true;
          raw.textContent = 'Show the record';
        }
      }
      return;
    }

    var vote = t.closest('[data-vote]');
    if (vote) {
      var box = vote.closest('.ballot');
      var how = vote.getAttribute('data-vote');
      box.innerHTML = '<span class="label">Recorded</span><p class="bq">' + (how === 'yes' ? 'Your vote is in: the hosting reading stands, with notice-and-action and statement-of-reasons duties.' : 'Another round is asked for, and the question stays open.') + '</p><p class="tiny" style="margin-top:8px">In the book this would write the ballot decision and the record that follows it.</p>';
      return;
    }

    var open = t.closest('[data-go]');
    if (open) { location.hash = '#/' + open.getAttribute('data-go'); return; }

    var go = t.closest('[data-more]');
    if (go) { showAllDecisions = true; route(); return; }
  });

  document.getElementById('searchBtn').addEventListener('click', openPalette);

  document.getElementById('themeBtn').addEventListener('click', function () {
    var root = document.documentElement;
    var next = root.getAttribute('data-theme') === 'night' ? 'paper' : 'night';
    root.setAttribute('data-theme', next);
    localStorage.setItem('book.theme', next);
    this.setAttribute('aria-label', next === 'night' ? 'Switch to the paper theme' : 'Switch to the night theme');
  });

  document.addEventListener('keydown', function (e) {
    var typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName);
    if ((e.key === 'k' || e.key === 'K') && (e.metaKey || e.ctrlKey)) { e.preventDefault(); palette.open ? closePalette() : openPalette(); return; }
    if (e.key === '/' && !typing) { e.preventDefault(); openPalette(); return; }
    if (e.key === 'Escape') { if (palette.open) closePalette(); return; }
    if (palette.open) {
      if (e.key === 'ArrowDown') { e.preventDefault(); movePalette(1); }
      if (e.key === 'ArrowUp') { e.preventDefault(); movePalette(-1); }
      if (e.key === 'Enter') { e.preventDefault(); choosePalette(); }
      return;
    }
    if (typing) return;
    var n2 = Number((location.hash.match(/^#\/read\/(\d+)/) || [])[1] || 0);
    if (n2 && (e.key === 'j' || e.key === 'ArrowRight')) { e.preventDefault(); location.hash = '#/read/' + Math.min(D.pages.length, n2 + 1); }
    if (n2 && (e.key === 'k' || e.key === 'ArrowLeft')) { e.preventDefault(); location.hash = '#/read/' + Math.max(1, n2 - 1); }
  });

  window.addEventListener('hashchange', route);

  function syncChrome() {
    var el = document.querySelector('.chrome');
    if (el) document.documentElement.style.setProperty('--chrome-h', el.offsetHeight + 'px');
  }
  window.addEventListener('resize', syncChrome);

  /* ---------- boot ---------- */
  var theme = localStorage.getItem('book.theme') || (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'night' : 'paper');
  document.documentElement.setAttribute('data-theme', theme);
  $('#projName').textContent = D.project.title;
  $('#tabToday').textContent = n(D.ballots.length + D.metrics.decisionsQueued);
  $('#tabChecks').textContent = n(D.metrics.findings);
  route();
  syncChrome();
})();
