// One audit pass over the rendered page. The browser evaluates this function with the
// primary selector and reads back a JSON array of { code, selector, message } findings.
// Each detector below names one audit code; the C# side attaches the route, width and
// scheme and merges identical findings across widths.
(primarySelector) => {
  const findings = [];

  const report = (code, selector, message) => {
    findings.push({ code, selector, message });
  };

  const transparent = (color) => color === 'rgba(0, 0, 0, 0)' || color === 'transparent';

  const visible = (element) => {
    if (!(element instanceof Element)) {
      return false;
    }

    const rect = element.getBoundingClientRect();

    if (rect.width === 0 || rect.height === 0) {
      return false;
    }

    const style = getComputedStyle(element);

    return style.display !== 'none' && style.visibility !== 'hidden' && style.visibility !== 'collapse';
  };

  const backgroundOf = (element) => getComputedStyle(element).backgroundColor;

  const hasBackground = (element) => !transparent(backgroundOf(element));

  const sideMax = (element, names, read) => {
    const style = getComputedStyle(element);
    let max = 0;

    for (const name of names) {
      max = Math.max(max, read(style, name));
    }

    return max;
  };

  const sides = ['Top', 'Right', 'Bottom', 'Left'];

  const hasBorder = (element) => {
    const style = getComputedStyle(element);

    return sides.some(
      (side) => parseFloat(style['border' + side + 'Width']) > 0 && style['border' + side + 'Style'] !== 'none');
  };

  const radiusOf = (element) => sideMax(
    element,
    ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'],
    (style, name) => parseFloat(style[name]));

  const paddingOf = (element) => sideMax(
    element,
    ['paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft'],
    (style, name) => parseFloat(style[name]));

  // A surface reads as a box: its own fill and border, rounded or padded. An outline
  // alone never qualifies, so focus rings cannot report here.
  const isBox = (element) => visible(element)
    && hasBackground(element)
    && hasBorder(element)
    && (radiusOf(element) > 0 || paddingOf(element) > 0);

  // Elements the nesting check never reads as a surface.
  const nestingIgnored = (element) => element.matches(
    'input,select,textarea,button,[role="button"],table,thead,tbody,tfoot,tr,td,th,script,style,noscript,template')
    || (element.getBoundingClientRect().height < 32 && getComputedStyle(element).display.startsWith('inline'));

  const selectorOf = (element) => {
    if (!(element instanceof Element)) {
      return 'page';
    }

    if (element.id) {
      return '#' + (window.CSS && window.CSS.escape ? window.CSS.escape(element.id) : element.id);
    }

    const parts = [];
    let node = element;

    while (node && node.nodeType === 1 && parts.length < 4 && node.tagName !== 'BODY' && node.tagName !== 'HTML') {
      let part = node.tagName.toLowerCase();
      const parent = node.parentElement;

      if (parent) {
        const siblings = Array.from(parent.children).filter((child) => child.tagName === node.tagName);

        if (siblings.length > 1) {
          part += ':nth-of-type(' + (siblings.indexOf(node) + 1) + ')';
        }
      }

      parts.unshift(part);
      node = parent;
    }

    return parts.join(' > ');
  };

  const nearestBackground = (element) => {
    let node = element.parentElement;

    while (node) {
      if (hasBackground(node)) {
        return backgroundOf(node);
      }

      node = node.parentElement;
    }

    return null;
  };

  // A box inside an ancestor box: the inner surface draws its own border.
  for (const element of document.querySelectorAll('*')) {
    if (!isBox(element) || nestingIgnored(element)) {
      continue;
    }

    let ancestor = element.parentElement;

    while (ancestor && ancestor.tagName !== 'BODY' && ancestor.tagName !== 'HTML') {
      if (isBox(ancestor) && !nestingIgnored(ancestor)) {
        report('surface-nesting-invalid', selectorOf(element), 'A surface inside another surface draws its own border.');
        break;
      }

      ancestor = ancestor.parentElement;
    }
  }

  // A header, a marked surface, or a padded rounded box that shares its fill with the
  // surface behind it while drawing no border of its own.
  const contrastCandidates = new Set(document.querySelectorAll('thead,[data-surface]'));

  for (const element of document.querySelectorAll('*')) {
    if (visible(element) && paddingOf(element) > 0 && radiusOf(element) > 0) {
      contrastCandidates.add(element);
    }
  }

  for (const element of contrastCandidates) {
    if (!visible(element)) {
      continue;
    }

    const behind = nearestBackground(element);

    if (behind === null) {
      continue;
    }

    const own = hasBackground(element) ? backgroundOf(element) : behind;

    if (own === behind && !hasBorder(element)) {
      report('surface-contrast-failed', selectorOf(element), 'A surface shares its fill with the surface behind it.');
    }
  }

  // A field whose nearest filled ancestor is the page itself.
  for (const field of document.querySelectorAll('input,select,textarea')) {
    if ((field.tagName === 'INPUT' && field.type === 'hidden') || !visible(field)) {
      continue;
    }

    let filled = field.parentElement;

    while (filled && !hasBackground(filled)) {
      filled = filled.parentElement;
    }

    if (filled && (filled.tagName === 'BODY' || filled.tagName === 'HTML' || filled.tagName === 'MAIN')) {
      report('field-surface-missing', selectorOf(field), 'A field sits on the page background.');
    }
  }

  // A short-valued field that spans the form: a code or short entry, a date, or a number.
  for (const field of document.querySelectorAll('input')) {
    if (!visible(field) || field.type === 'hidden') {
      continue;
    }

    const limit = parseInt(field.getAttribute('maxlength') || '', 10);
    const short = (Number.isFinite(limit) && limit <= 10)
      || field.type === 'date'
      || field.type === 'number'
      || field.getAttribute('inputmode') === 'decimal'
      || field.getAttribute('inputmode') === 'numeric';

    if (!short) {
      continue;
    }

    const form = field.closest('form');

    if (!form) {
      continue;
    }

    const width = field.getBoundingClientRect().width;
    const formWidth = form.getBoundingClientRect().width;

    if (width > 320 && formWidth > 0 && width > 0.6 * formWidth) {
      report('field-width-invalid', selectorOf(field), 'A short field spans the form.');
    }
  }

  // Sibling buttons in one flex row: one size, and never one action alone on the last line.
  const buttonSelector = 'button,input[type="button"],input[type="submit"],input[type="reset"],[role="button"]';

  for (const group of document.querySelectorAll('*')) {
    if (!(group instanceof Element)) {
      continue;
    }

    const style = getComputedStyle(group);

    if ((style.display !== 'flex' && style.display !== 'inline-flex') || style.flexDirection !== 'row') {
      continue;
    }

    const buttons = Array.from(group.children)
      .filter((child) => child instanceof Element && child.matches(buttonSelector) && visible(child));

    if (buttons.length < 2) {
      continue;
    }

    if (new Set(buttons.map((button) => button.offsetHeight)).size > 1) {
      report('action-group-invalid', selectorOf(group), 'Actions in one group have different sizes.');
    }

    if (style.flexWrap !== 'nowrap') {
      const rows = new Map();

      for (const button of buttons) {
        const row = rows.get(button.offsetTop) || [];
        row.push(button);
        rows.set(button.offsetTop, row);
      }

      if (rows.size > 1) {
        const tops = Array.from(rows.keys()).sort((left, right) => left - right);
        const last = rows.get(tops[tops.length - 1]) || [];

        if (last.length === 1 && tops.slice(0, -1).some((top) => (rows.get(top) || []).length >= 2)) {
          report('action-group-invalid', selectorOf(group), 'An action group strands one action on its own row.');
        }
      }
    }
  }

  // More than one primary action inside the main landmark.
  const scope = document.querySelector('main') || document.body;
  let primaries = [];

  try {
    primaries = Array.from(scope.querySelectorAll(primarySelector)).filter(visible);
  } catch (error) {
    throw new Error("The primary selector '" + primarySelector + "' is not valid CSS: " + error.message);
  }

  if (primaries.length > 1) {
    report('primary-action-duplicate', selectorOf(scope), 'The view shows ' + primaries.length + ' primary actions.');
  }

  // A column whose body cells mostly parse as numbers or money but aligns to the start.
  // Currency signs stay as escapes so this file holds ASCII only.
  const money = /^\s*(?:[A-Z]{3}\s?|[$\u20AC\u00A3\u00A5\u20B9]\s?)?[-+]?[0-9][0-9.,\s\u00A0\u202F]*\s*(?:[A-Z]{3}|[%])?\s*$/;

  for (const table of document.querySelectorAll('table')) {
    if (!visible(table)) {
      continue;
    }

    const rows = Array.from(table.querySelectorAll('tr'))
      .filter((row) => !row.closest('thead') && !row.closest('tfoot'));

    const widths = rows.map(
      (row) => Array.from(row.cells).filter((cell) => cell.tagName === 'TD').length);
    const columns = widths.length === 0 ? 0 : Math.max.apply(null, widths);

    for (let column = 0; column < columns; column++) {
      const cells = [];

      for (const row of rows) {
        const data = Array.from(row.cells).filter((cell) => cell.tagName === 'TD');

        if (data[column]) {
          cells.push(data[column]);
        }
      }

      if (cells.length === 0) {
        continue;
      }

      const numeric = cells.filter((cell) => money.test((cell.innerText || '').trim()));

      if (numeric.length > cells.length / 2) {
        const align = getComputedStyle(numeric[0]).textAlign;

        if (align === 'start' || align === 'left') {
          report(
            'column-alignment-invalid',
            selectorOf(table) + ' td:nth-child(' + (column + 1) + ')',
            'A numeric column aligns to the start.');
        }
      }
    }
  }

  // Visible text that is a raw code: a GUID, a dotted message key, null, undefined, or (null).
  const guid = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
  const messageKey = /^[a-z]+(\.[a-zA-Z]+)+$/;
  const hiddenParent = 'SCRIPT,STYLE,NOSCRIPT,TEMPLATE';

  const walker = document.createTreeWalker(document.body || document, NodeFilter.SHOW_TEXT, {
    acceptNode(node) {
      const parent = node.parentElement;

      if (!parent || hiddenParent.split(',').includes(parent.tagName) || !visible(parent)) {
        return NodeFilter.FILTER_REJECT;
      }

      return (node.nodeValue || '').trim().length === 0 ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT;
    },
  });

  let text = walker.nextNode();

  while (text) {
    const shown = (text.nodeValue || '').trim();
    const excerpt = shown.length > 60 ? shown.slice(0, 57) + '...' : shown;

    if (guid.test(shown) || messageKey.test(shown) || shown === 'null' || shown === 'undefined'
      || shown === '(null)' || shown.includes('(null)')) {
      report('text-code-invalid', selectorOf(text.parentElement), "The page shows the code '" + excerpt + "'.");
    }

    text = walker.nextNode();
  }

  // A tab label that wraps, or a tab list that wraps to two rows.
  for (const list of document.querySelectorAll('[role="tablist"]')) {
    if (!visible(list)) {
      continue;
    }

    const tabs = Array.from(list.querySelectorAll('[role="tab"]')).filter(visible);

    if (new Set(tabs.map((tab) => tab.offsetTop)).size > 1) {
      report('tab-label-invalid', selectorOf(list), 'A tab list wraps to two rows.');
    }

    for (const tab of tabs) {
      if (tab.scrollHeight > tab.clientHeight + 1) {
        report('tab-label-invalid', selectorOf(tab), 'A tab label wraps.');
      }
    }
  }

  // Below 600 pixels, a table wider than its container with no scrolling ancestor up to main.
  if (window.innerWidth < 600) {
    const main = document.querySelector('main');
    const top = main || document.body;

    for (const table of document.querySelectorAll('table')) {
      if (!visible(table) || !table.parentElement) {
        continue;
      }

      if (table.getBoundingClientRect().width <= table.parentElement.clientWidth + 1) {
        continue;
      }

      let scrolls = false;
      let ancestor = table.parentElement;

      while (ancestor && ancestor !== top.parentElement) {
        const overflow = getComputedStyle(ancestor).overflowX;

        if (overflow === 'auto' || overflow === 'scroll') {
          scrolls = true;
          break;
        }

        if (ancestor === top) {
          break;
        }

        ancestor = ancestor.parentElement;
      }

      if (!scrolls) {
        report('table-clip-invalid', selectorOf(table), 'A table is wider than its container and no ancestor scrolls.');
      }
    }
  }

  return JSON.stringify(findings);
};
