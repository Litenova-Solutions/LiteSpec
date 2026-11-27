<script lang="ts">
  // The field grid of the __PROJECT__ shared UI package: the aligned 12
  // columns form fields sit on, in groups of at most 6 fields. Each field
  // the route places names the columns it spans.
  import type { Snippet } from "svelte";
  import { cn } from "../../utils/cn.ts";

  type Props = {
    children: Snippet;
    class?: string;
  };

  let { children, class: className = "" }: Props = $props();
</script>

<div class={cn("grid grid-cols-12 gap-4", className)}>
  {@render children()}
</div>
