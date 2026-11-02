const http = require('http');
const fs = require('fs');
const path = require('path');

const root = process.argv[2] || '.';
const port = Number(process.argv[3] || 8765);
const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.woff2': 'font/woff2',
};

http
  .createServer((req, res) => {
    const url = decodeURIComponent(req.url.split('?')[0]);
    const file = path.join(root, url === '/' ? 'index.html' : url);
    fs.readFile(file, (err, body) => {
      if (err) {
        res.writeHead(404, { 'content-type': 'text/plain' });
        res.end('not found: ' + url);
        return;
      }
      res.writeHead(200, { 'content-type': types[path.extname(file)] || 'application/octet-stream' });
      res.end(body);
    });
  })
  .listen(port, () => console.log('serving ' + root + ' on http://localhost:' + port));
