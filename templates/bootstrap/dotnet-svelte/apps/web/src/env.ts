import { defineEnvVars } from "@sveltejs/kit/env";

// The backend address the server code of the __PROJECT__ web app reaches:
// the compose and AppHost value when set, the local API port otherwise.
export const variables = defineEnvVars({
  __NAMESPACE___BACKEND_URL: {
    description:
      "The server-side backend address the web app forwards API and sign-in calls to.",
    schema: (value) => (value === undefined || value === "" ? "http://localhost:5005" : value),
  },
});
