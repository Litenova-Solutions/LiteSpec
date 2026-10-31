// Lists every property name in record.schema.json with the $defs that declare it and the value shape.
const fs = require('fs');
const s = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const map = new Map();
function shape(p) {
  if (!p || typeof p !== 'object') return '?';
  if (p.$ref) return p.$ref.replace('#/$defs/', '');
  if (p.const !== undefined) return '=' + p.const;
  if (p.enum) return 'enum(' + p.enum.join('|') + ')';
  if (p.type === 'array') return '[' + shape(p.items) + ']';
  if (p.oneOf || p.anyOf) return (p.oneOf || p.anyOf).map(shape).join(' or ');
  return p.type || 'obj';
}
function walk(node, def) {
  if (!node || typeof node !== 'object') return;
  if (node.properties) for (const [k, v] of Object.entries(node.properties)) {
    if (!map.has(k)) map.set(k, new Map());
    map.get(k).set(def, shape(v));
    walk(v, def + '.' + k);
  }
  if (node.patternProperties) for (const v of Object.values(node.patternProperties)) walk(v, def);
  for (const key of ['items', 'additionalProperties', 'contains', 'then', 'else', 'if', 'not']) if (node[key]) walk(node[key], def);
  for (const key of ['allOf', 'oneOf', 'anyOf', 'prefixItems']) if (Array.isArray(node[key])) node[key].forEach(n => walk(n, def));
}
for (const [name, def] of Object.entries(s.$defs || {})) walk(def, name);
const rows = [...map.entries()].sort((a, b) => a[0].localeCompare(b[0]));
for (const [k, defs] of rows) {
  const shapes = [...new Set(defs.values())];
  console.log(k + '\t' + defs.size + '\t' + shapes.join(' ; ').slice(0, 90) + '\t' + [...defs.keys()].map(d => d.replace(/Page|Entry|Value|Object/g, '')).join(',').slice(0, 140));
}
console.error(rows.length + ' field names');
