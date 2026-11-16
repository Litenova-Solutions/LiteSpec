<script lang="ts">
  import { cn } from "$lib/frame";
  import { guideLeft } from "./outline-rows";

  // One row's tree guide lines: a thin vertical line at each ancestor's
  // indent, running the row's full height while that branch continues below
  // it and ending halfway down the row where the branch ends. The branch
  // holding the selected record, or a hovered row, reads a little stronger.
  type Props = { guides: boolean[]; strong?: boolean };
  let { guides, strong = false }: Props = $props();
</script>

{#each guides as full, i}
  <span
    aria-hidden="true"
    class={cn(
      "pointer-events-none absolute w-px",
      full ? "top-0 bottom-0" : "top-0 h-1/2",
      strong ? "bg-border-strong" : "bg-border-subtle group-hover:bg-border-strong",
    )}
    style="left: {guideLeft(i + 1)}px"
  ></span>
{/each}
