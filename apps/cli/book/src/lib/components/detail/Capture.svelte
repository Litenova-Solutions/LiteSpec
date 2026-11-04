<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { Button } from "$lib/components/ui/button";

  // The screenshot of a screen, with width and theme toggles kept per record.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const captures = $derived(((r.captures as Fields[] | undefined) ?? []) as Fields[]);
  const captureState = $derived(String((r.captureState as string | undefined) ?? (captures.length ? "captured" : "")));
  const routeText = $derived.by(() => {
    const rt = r.route as Fields | undefined;
    return String(rt?.route ?? rt?.path ?? (r.fields?.address as string | undefined) ?? "");
  });

  let choice = $state<Record<string, { width: string; theme: string }>>({});
  const ch = $derived(choice[id] ?? { width: "desktop", theme: "light" });

  const current = $derived.by(() => {
    const wantWidth = ch.width === "phone" ? 390 : 1440;
    return (
      captures.filter((c) => c.theme === ch.theme && (+c.width === wantWidth || (ch.width === "phone" ? +c.width < 600 : +c.width >= 600)))[0] ??
      captures.filter((c) => c.theme === ch.theme)[0] ??
      captures[0]
    );
  });
  const others = $derived(captures.filter((c) => c !== current));
  const src = $derived(String(current?.bookPath ?? current?.path ?? ""));
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
  {#each others as c}
    <link rel="prefetch" href={String(c.bookPath ?? c.path ?? "")} as="image" />
  {/each}
</svelte:head>

{#if !captures.length}
  {#if captureState === "route-not-captured"}
    <p class="mt-0 mb-6 text-[14.5px] text-subtle-foreground">The route exists but was not captured.</p>
  {:else if captureState === "no-route"}
    <p class="mt-0 mb-6 text-[14.5px] text-subtle-foreground">No page carries this screen.</p>
  {:else if captureState}
    <p class="mt-0 mb-6 text-[14.5px] text-subtle-foreground">Capture captureState: {captureState}.</p>
  {:else if r.route}
    <p class="mt-0 mb-6 text-[14.5px] text-subtle-foreground">The route exists but was not captured.</p>
  {:else}
    <p class="mt-0 mb-6 text-[14.5px] text-subtle-foreground">No page carries this screen.</p>
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
