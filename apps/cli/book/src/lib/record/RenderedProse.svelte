<script lang="ts">
  import { cn } from "$lib/frame";
  import { glossary } from "./glossary-links";

  // Rendered prose from the CLI or from inline(): typography from the Tailwind
  // plugin, record links as underlined buttons, glossary terms linked on first use.
  let { html, class: className = "", lead = false }: { html: string; class?: string; lead?: boolean } = $props();
</script>

{#if html}
  <div
    use:glossary={html}
    class={cn(
      "prose prose-stone max-w-none dark:prose-invert prose-a:underline-offset-2 prose-code:before:content-none prose-code:after:content-none",
      "[&_.book-ref]:underline [&_.book-ref]:decoration-strong [&_.book-ref]:underline-offset-2 [&_.book-ref:hover]:decoration-foreground",
      "[&_.book-glossary]:underline [&_.book-glossary]:decoration-highlight [&_.book-glossary]:decoration-dotted [&_.book-glossary]:underline-offset-2",
      lead ? "prose-lg" : "prose-sm sm:prose-base",
      className,
    )}
  >
    {@html html}
  </div>
{/if}
