import adapter from "@sveltejs/adapter-node";
import { sveltekit, type Config } from "@sveltejs/kit/vite";
import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig, loadEnv } from "vite";

// The one Content Security Policy of the __PROJECT__ web host. It carries
// the Trusted Types directive the stack asks for, and names the
// __SCOPE__-sanitizer as the single policy for raw markup. No raw markup
// sink exists in the app yet, so that policy has no callers.
//
// The policy is enforced, and mode 'auto' lets SvelteKit add the nonce to
// the scripts and styles it renders. style-src keeps 'unsafe-inline' on
// purpose: app.html carries an inline style attribute, which a nonce cannot
// allow, and Svelte injects component styles at runtime.
const contentSecurityPolicy: NonNullable<Config["csp"]> = {
  mode: "auto",
  directives: {
    "default-src": ["self"],
    "script-src": ["self"],
    "style-src": ["self", "unsafe-inline"],
    "img-src": ["self", "data:"],
    "font-src": ["self"],
    "connect-src": ["self"],
    "object-src": ["none"],
    "base-uri": ["self"],
    "frame-ancestors": ["none"],
    "form-action": ["self"],
    "require-trusted-types-for": ["script"],
    "trusted-types": ["svelte-trusted-html", "__SCOPE__-sanitizer", "allow-duplicates"],
    "upgrade-insecure-requests": true,
  },
};

// The __PROJECT__ web build: SvelteKit with the node adapter for the
// container image, styled through the shared UI tokens. SvelteKit 3 reads
// its configuration from the sveltekit plugin below; there is no
// svelte.config file.
export default defineConfig(({ mode }) => {
  // The development proxy target: the Aspire AppHost sets
  // __NAMESPACE___BACKEND_URL to the API endpoint; a run without it reaches
  // the API launch profile. loadEnv with an empty prefix reads the process
  // environment as well.
  const backend = loadEnv(mode, ".", "").__NAMESPACE___BACKEND_URL ?? "http://localhost:5005";
  return {
    plugins: [
      tailwindcss(),
      sveltekit({
        adapter: adapter(),
        csp: contentSecurityPolicy,
        preprocess: vitePreprocess(),
      }),
    ],
    // The bundle uses top-level await, which Vite's default browser target
    // refuses; every browser the starter supports runs ES2022.
    build: {
      target: "es2022",
    },
    server: {
      proxy: {
        "/api": backend,
        "/auth": backend,
      },
    },
  };
});
