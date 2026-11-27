<script lang="ts">
  // The action bar of the __PROJECT__ shared UI package: one row for the
  // primary, secondary, and danger actions of a page in matching sizes, with
  // 8px between controls. The route orders its actions and names one primary.
  import type { Snippet } from "svelte";
  import { cn } from "../../utils/cn.ts";

  type Props = {
    children: Snippet;
    class?: string;
  };

  let { children, class: className = "" }: Props = $props();
</script>

<div class={cn("flex flex-wrap items-center gap-2", className)}>
  {@render children()}
</div>
