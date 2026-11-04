<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import Block from "./blocks/Block.svelte";

  // The completion block: whether the record has code and tests, and how many
  // of its scenarios pass. Each line shows only when the record expects it.
  let { r }: { r: BookRecord } = $props();
  const c = $derived(r.completion ?? {});
  const run = $derived(book.man?.testRun);
  const show = $derived(c.hasCode !== null && c.hasCode !== undefined || c.hasTests !== null && c.hasTests !== undefined || !!c.scenarios);
</script>

{#if show}
  <Block blockKey="completion" heading="Completion">
    <ul class="m-0 list-none p-0">
      {#if c.hasCode !== null && c.hasCode !== undefined}
        <li class="flex items-center gap-2">
          <Icon name={c.hasCode ? "done" : "missing"} class="size-3.5 {c.hasCode ? 'text-convention-foreground' : 'text-warn-foreground'}" />
          <span class={c.hasCode ? "" : "text-warn-foreground"}>{c.hasCode ? "Has code" : "No code"}</span>
        </li>
      {/if}
      {#if c.hasTests !== null && c.hasTests !== undefined}
        <li class="flex items-center gap-2">
          <Icon name={c.hasTests ? "done" : "missing"} class="size-3.5 {c.hasTests ? 'text-convention-foreground' : 'text-warn-foreground'}" />
          <span class={c.hasTests ? "" : "text-warn-foreground"}>{c.hasTests ? "Has tests" : "No tests"}</span>
        </li>
      {/if}
      {#if c.scenarios}
        {@const pass = c.scenarios.pass ?? 0}
        {@const of = c.scenarios.of ?? 0}
        {@const all = of > 0 && pass >= of}
        <li class="flex items-center gap-2">
          <Icon name={all ? "done" : "missing"} class="size-3.5 {all ? 'text-convention-foreground' : 'text-warn-foreground'}" />
          <span class={all ? "" : "text-warn-foreground"}>{pass} of {of} {of === 1 ? "scenario passes" : "scenarios pass"}{#if !run} (no test run in the model){/if}</span>
        </li>
      {/if}
    </ul>
  </Block>
{/if}
