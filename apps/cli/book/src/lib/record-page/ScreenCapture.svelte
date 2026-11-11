<script lang="ts">
  import { book, formatLabel } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { Button } from "$lib/ui/button";
  import { pickCapture, routeTextOf, stringField } from "./page-values";

  // The screenshot of a screen, with width and theme toggles kept per record.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const captures = $derived(r.captures ?? []);
  const captureState = $derived(r.captureState ?? (captures.length ? "captured" : ""));
  const routeText = $derived(routeTextOf(r.route) ?? stringField(r.fields ?? {}, "address") ?? "");

  let choice = $state<Record<string, { width: string; theme: string }>>({});
  const ch = $derived(choice[id] ?? { width: "desktop", theme: "light" });

  const current = $derived(pickCapture(captures, ch));
  const others = $derived(captures.filter((c) => c !== current));
  const src = $derived(current ? String(current.bookPath ?? current.path) : "");
  const caption = $derived(
    [
      routeText ? "Route: " + routeText : "",
      "Theme: " + String(current?.theme ?? ""),
      "Width: " + String(current?.width ?? ""),
      current?.capturedOn ? "Captured: " + book.fmtDate(String(current.capturedOn)) : "",
    ]
      .filter(Boolean)
      .join(", "),
  );

  function set(kind: "width" | "theme", value: string) {
    choice[id] = { ...ch, [kind]: value };
  }
</script>

<svelte:head>
  {#each others as c (c.path)}
    <link rel="prefetch" href={String(c.bookPath ?? c.path ?? "")} as="image" />
  {/each}
</svelte:head>

{#if !captures.length}
  {#if captureState === "route-not-captured"}
    <p class="mt-0 mb-6 text-[0.90625rem] text-subtle-foreground">The route exists but was not captured.</p>
  {:else if captureState === "no-route"}
    <p class="mt-0 mb-6 text-[0.90625rem] text-subtle-foreground">No page carries this screen.</p>
  {:else if captureState}
    <p class="mt-0 mb-6 text-[0.90625rem] text-subtle-foreground">Capture state: {formatLabel(captureState)}.</p>
  {:else if r.route}
    <p class="mt-0 mb-6 text-[0.90625rem] text-subtle-foreground">The route exists but was not captured.</p>
  {:else}
    <p class="mt-0 mb-6 text-[0.90625rem] text-subtle-foreground">No page carries this screen.</p>
  {/if}
{:else}
  <figure class="m-0 mb-2">
    <img {src} alt={"Screenshot of " + r.title} loading="eager" class="block h-auto w-full rounded-md border border-border bg-muted" />
    <figcaption class="mt-2 text-xs text-muted-foreground">{caption}</figcaption>
  </figure>
  <div class="mt-2.5 mb-6 flex flex-wrap items-center gap-1.5" role="group" aria-label="Screenshot variant">
      <Button
        variant={ch.width === "desktop" ? "default" : "outline"}
        size="sm"
        aria-pressed={ch.width === "desktop"}
        onclick={() => set("width", "desktop")}
      >
        Desktop
      </Button>
      <Button
        variant={ch.width === "phone" ? "default" : "outline"}
        size="sm"
        aria-pressed={ch.width === "phone"}
        onclick={() => set("width", "phone")}
      >
        Phone
      </Button>
      <span class="w-2.5" aria-hidden="true"></span>
      <Button
        variant={ch.theme === "light" ? "default" : "outline"}
        size="sm"
        aria-pressed={ch.theme === "light"}
        onclick={() => set("theme", "light")}
      >
        Light
      </Button>
      <Button
        variant={ch.theme === "dark" ? "default" : "outline"}
        size="sm"
        aria-pressed={ch.theme === "dark"}
        onclick={() => set("theme", "dark")}
      >
        Dark
      </Button>
  </div>
{/if}
