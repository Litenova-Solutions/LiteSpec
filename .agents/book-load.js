// Loads the generated .book data shards into memory so they can be inspected.
const fs = require('fs');
const path = require('path');
const dir = process.env.BOOK_DATA || process.argv[2];
const store = {};
global.BookData = { put: (k, v) => (store[k] = v), get: (k) => store[k] };
const files = fs.readdirSync(dir).filter((f) => f.endsWith('.js'));
for (const f of files) {
  const src = fs.readFileSync(path.join(dir, f), 'utf8');
  try {
    new Function(src)();
  } catch (e) {
    console.error('failed', f, e.message);
  }
}
module.exports = store;
