<script lang="ts">
  import { book, formatLabel } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { pickCapture, routeTextOf, stringField } from "./page-values";

  // The screenshot of a screen: the desktop light capture with its route and capture date.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const captures = $derived(r.captures ?? []);
  const captureState = $derived(r.captureState ?? (captures.length ? "captured" : ""));
  const routeText = $derived(routeTextOf(r.route) ?? stringField(r.fields ?? {}, "address") ?? "");

  const current = $derived(pickCapture(captures, { width: "desktop", theme: "light" }));
  const src = $derived(current ? String(current.bookPath ?? current.path) : "");
  const caption = $derived(
    [
      routeText ? "Route: " + routeText : "",
      current?.capturedOn ? "Captured: " + book.fmtDate(String(current.capturedOn)) : "",
    ]
      .filter(Boolean)
      .join(", "),
  );
</script>

{#if !captures.length}
  {#if captureState === "route-not-captured"}
    <p class="mt-0 mb-6 text-sm text-slate-600">The route exists but was not captured.</p>
  {:else if captureState === "no-route"}
    <p class="mt-0 mb-6 text-sm text-slate-600">No page carries this screen.</p>
  {:else if captureState}
    <p class="mt-0 mb-6 text-sm text-slate-600">Capture state: {formatLabel(captureState)}.</p>
  {:else if r.route}
    <p class="mt-0 mb-6 text-sm text-slate-600">The route exists but was not captured.</p>
  {:else}
    <p class="mt-0 mb-6 text-sm text-slate-600">No page carries this screen.</p>
  {/if}
{:else}
  <figure class="m-0 mb-6">
    <img {src} alt={"Screenshot of " + r.title} loading="eager" class="block h-auto w-full rounded-lg border border-slate-300 bg-slate-100" />
    {#if caption}
      <figcaption class="mt-2 text-xs text-slate-600">{caption}</figcaption>
    {/if}
  </figure>
{/if}
