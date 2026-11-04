import { svelte } from "@sveltejs/vite-plugin-svelte";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import path from "node:path";
import { defineConfig, type Plugin } from "vite";

// The book opens from file://, where module scripts and fetch are blocked, so the
// build is one classic script and one stylesheet with every font inlined.
// In development, BOOK_DATA names a written book's data folder to serve as /data.
function bookData(): Plugin {
  const dir = process.env.BOOK_DATA;
  return {
    name: "book-data",
    configureServer(server) {
      if (!dir) return;
      server.middlewares.use("/data", (req, res, next) => {
        const file = path.join(dir, decodeURIComponent((req.url ?? "").split("?")[0]));
        if (!file.startsWith(path.resolve(dir)) || !fs.existsSync(file)) return next();
        res.setHeader("Content-Type", "text/javascript");
        fs.createReadStream(file).pipe(res);
      });
    },
  };
}

export default defineConfig(({ command }) => ({
  // Library mode keeps process.env reads, which a browser lacks; a build is always production.
  define: command === "build" ? { "process.env.NODE_ENV": JSON.stringify("production") } : {},
  plugins: [tailwindcss(), svelte(), bookData()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
    cssCodeSplit: false,
    assetsInlineLimit: 100_000_000,
    lib: { entry: "src/main.ts", name: "LiteSpecBook", formats: ["iife"], fileName: () => "book.js", cssFileName: "book" },
  },
  // Component tests mount in jsdom, so Svelte resolves its browser build.
  resolve: { alias: { $lib: fileURLToPath(new URL("./src/lib", import.meta.url)) }, ...(process.env.VITEST ? { conditions: ["browser"] } : {}) },
  // A test reads the record type list from the model library elsewhere in the repository.
  server: { fs: { allow: [fileURLToPath(new URL("../../..", import.meta.url))] } },
  test: { environment: "jsdom", include: ["src/**/*.test.ts"] },
}));
