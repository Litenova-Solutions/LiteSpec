<script lang="ts">
  import { parseWireframe } from "./parse";
  import { renderWireframe } from "./render";

  // One wireframe figure: the bare drawing, which the screen and storyboard frame, or a short state when
  // the screen carries no wireframe text or the text has errors.
  type Props = { text: string | null | undefined; title: string };
  let { text, title }: Props = $props();
  const parsed = $derived(text ? parseWireframe(text) : null);
  const svg = $derived(parsed?.page ? renderWireframe(parsed.page) : null);
</script>

<figure class="m-0 overflow-hidden bg-background" aria-label={"Wireframe of " + title}>
  {#if svg}
    <div class="[&>svg]:block [&>svg]:h-auto [&>svg]:w-full">{@html svg}</div>
  {:else}
    <div class="px-4 py-6 text-center">
      <div class="text-sm font-medium text-foreground">{title}</div>
      <p class="m-0 mt-1 text-xs text-text-secondary">{text ? "The wireframe has errors." : "No wireframe drawn."}</p>
    </div>
  {/if}
</figure>
