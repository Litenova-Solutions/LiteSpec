// node build.js <title> <out> <part> [<part> ...]   parts are files; grid placeholders filled from provgrid.html
const fs = require('fs');
const [,, title, out, ...parts] = process.argv;
const dir = __dirname;
const grid = fs.readFileSync(dir + '/provgrid.html', 'utf8');
const a = grid.split('<!--A-->')[1].split('<!--B-->')[0];
const b = grid.split('<!--B-->')[1].split('<!--C-->')[0];
const c = grid.split('<!--C-->')[1].split('<!--STATS')[0];
let html = fs.readFileSync(dir + '/head.html', 'utf8').replace('__TITLE__', title);
for (const p of parts) html += fs.readFileSync(dir + '/' + p, 'utf8');
html += fs.readFileSync(dir + '/foot.html', 'utf8');
html = html.replace('__GRID_A__', a).replace('__GRID_B__', b).replace('__GRID_C__', c);
if (/[^\x00-\x7F]/.test(html.replace(/<style>[\s\S]*?<\/style>/, ''))) console.error('warning: non-ASCII characters');
fs.writeFileSync(out, html);
console.log(out, html.length);
