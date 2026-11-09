// Serves a folder over HTTP on 127.0.0.1 so the install scripts can download from it the way they
// download a GitHub release. The system picks a free port, and the first line printed names it:
//
//   node .github/scripts/serve.mjs <folder>
//   port 54321
import { createReadStream, statSync } from "node:fs";
import { createServer } from "node:http";
import { join, normalize, sep } from "node:path";

const root = normalize(process.argv[2] ?? ".");

const server = createServer((request, response) => {
  const path = decodeURIComponent(new URL(request.url ?? "/", "http://127.0.0.1").pathname);
  const file = normalize(join(root, path));

  // A path that climbs out of the folder is refused rather than served.
  if (file !== root && !file.startsWith(root.endsWith(sep) ? root : root + sep)) {
    response.writeHead(403).end();
    return;
  }

  let size;

  try {
    const stat = statSync(file);

    if (!stat.isFile()) {
      throw new Error("not a file");
    }

    size = stat.size;
  } catch {
    response.writeHead(404).end();
    return;
  }

  response.writeHead(200, { "Content-Length": size, "Content-Type": "application/octet-stream" });

  if (request.method === "HEAD") {
    response.end();
    return;
  }

  createReadStream(file).pipe(response);
});

server.listen(0, "127.0.0.1", () => {
  console.log(`port ${server.address().port}`);
});
