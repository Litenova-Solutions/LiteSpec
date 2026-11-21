import adapter from "@sveltejs/adapter-static";
import { sveltekit } from "@sveltejs/kit/vite";
import { defineConfig } from "vite";

export default defineConfig({
  // The console is a single-page app behind sign-in, so every address falls
  // back to the app shell.
  plugins: [sveltekit({ adapter: adapter({ fallback: "index.html" }) })],
});
