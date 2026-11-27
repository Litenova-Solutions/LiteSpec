<script lang="ts">
  // The subsection of the __PROJECT__ shared UI package: the allowed divider
  // for one card that truly holds two labeled groups. Anything deeper nests
  // surfaces, so a second subsection inside one never appears.
  import type { Snippet } from "svelte";
  import { cn } from "../../utils/cn.ts";

  type Props = {
    title: string;
    children: Snippet;
    class?: string;
  };

  let { title, children, class: className = "" }: Props = $props();
</script>

<section aria-label={title} class={cn("grid gap-4 border-t border-border pt-4", className)}>
  <h3 class="text-sm font-medium">{title}</h3>
  {@render children()}
</section>
