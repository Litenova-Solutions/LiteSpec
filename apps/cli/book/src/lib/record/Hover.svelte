<script lang="ts">
  import type { Snippet } from "svelte";
  import type { Hover } from "$lib/frame/hover-text";
  import * as Tooltip from "$lib/ui/tooltip";

  // One hover explanation for the whole book. The children render as the
  // tooltip trigger, keyboard focusable without nesting buttons, and the
  // tooltip shows the title in semibold on line one with the body below. A
  // caller that carries an explanation key puts data-explain on its own
  // content, so a click still opens the full explanation through ExplainLayer.
  // A block trigger fills its row: a navigation or trace row that is already
  // focusable itself, so the wrapper takes no extra tab stop.
  type Props = { hover: Hover; side?: "top" | "right" | "bottom" | "left"; block?: boolean; children: Snippet };
  let { hover, side = "top", block = false, children }: Props = $props();
</script>

<Tooltip.Root>
  <Tooltip.Trigger>
    {#snippet child({ props })}
      <!-- The trigger takes keyboard focus so keyboard readers get the explanation too. -->
      <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
      <span {...props} tabindex={block ? -1 : 0} data-hover class={block ? "block w-full min-w-0" : "inline-flex cursor-help items-center"}>
        {@render children()}
      </span>
    {/snippet}
  </Tooltip.Trigger>
  <Tooltip.Content {side} class="block">
    <span class="block text-xs font-semibold">{hover.title}</span>
    {#if hover.body}<span class="mt-0.5 block text-xs leading-4">{hover.body}</span>{/if}
  </Tooltip.Content>
</Tooltip.Root>
