import { defineConfig } from "@playwright/test";

// The route suite of the __PROJECT__ web app: every address the route tree
// discovers, visited in a browser. The application under test already runs
// (the development server with its API and database beside it), so the suite
// only visits addresses and reads what each page shows.
export default defineConfig({
  testDir: "./tests",
  testMatch: "**/*.spec.ts",
  use: {
    baseURL: process.env.PLAYWRIGHT_TEST_BASE_URL ?? "http://localhost:5173",
  },
});
