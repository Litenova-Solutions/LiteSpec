<script lang="ts">
  // The text link of the __PROJECT__ shared UI package: the one way a page
  // moves the reader to another address, with the shared focus state. A
  // button runs an action on the page; this moves the reader off it.
  import type { Snippet } from "svelte";
  import { cn } from "../../utils/cn.ts";

  type Props = {
    href: string;
    children: Snippet;
    external?: boolean;
    class?: string;
  };

  let { href, children, external = false, class: className = "" }: Props = $props();
</script>

<a
  {href}
  rel={external ? "noreferrer" : undefined}
  target={external ? "_blank" : undefined}
  class={cn("w-fit underline underline-offset-4 outline-none focus-visible:ring-2 focus-visible:ring-ring", className)}
>
  {@render children()}
</a>
