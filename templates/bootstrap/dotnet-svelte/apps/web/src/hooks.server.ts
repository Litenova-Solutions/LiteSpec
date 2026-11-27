import { __NAMESPACE___BACKEND_URL } from "$app/env/private";
import type { Handle, HandleFetch } from "@sveltejs/kit/hooks";

// The server hook of the __PROJECT__ web app: the browser calls the API and
// the development sign-in on the same origin, and this hook forwards those
// calls to the API host with the session cookie intact.

// The backend address the server code reaches, declared in src/env.ts from
// the compose and AppHost value when set, the local API port otherwise.
function backendAddress(): string {
  return __NAMESPACE___BACKEND_URL;
}

// Whether a path belongs to the API or the development sign-in, which both
// live behind this hook instead of beside the pages.
function isProxied(pathname: string): boolean {
  return (
    pathname === "/api" ||
    pathname.startsWith("/api/") ||
    pathname === "/auth" ||
    pathname.startsWith("/auth/")
  );
}

// Forwards one request to the backend, keeping its method, headers and body.
async function proxy(request: Request, target: string): Promise<Response> {
  const headers = new Headers(request.headers);
  headers.delete("host");
  const body =
    request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer();
  return fetch(target, { method: request.method, headers, body, redirect: "manual" });
}

// Forwards browser calls under /api and /auth to the API host. The page
// loads read the content type of the API answers they fetch, so the hook
// keeps that header when it serializes load responses.
export const handle: Handle = async ({ event, resolve }) => {
  if (isProxied(event.url.pathname)) {
    return proxy(event.request, `${backendAddress()}${event.url.pathname}${event.url.search}`);
  }
  return resolve(event, {
    filterSerializedResponseHeaders: (name) => name === "content-type",
  });
};

// Forwards server load calls under /api and /auth to the API host, so page
// loads reach the backend without leaving the server.
export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
  const target = new URL(request.url);
  if (target.origin === event.url.origin && isProxied(target.pathname)) {
    return proxy(request, `${backendAddress()}${target.pathname}${target.search}`);
  }
  return fetch(request);
};
