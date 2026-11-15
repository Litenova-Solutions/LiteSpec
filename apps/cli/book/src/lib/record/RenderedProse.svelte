<script lang="ts">
  import { cn } from "$lib/frame";
  import { glossaryLinks } from "./glossary-links";

  // Rendered prose from the CLI or from inline(): typography from the Tailwind
  // plugin, record links underlined in the accent, glossary terms linked on first use.
  type Props = { html: string; class?: string; lead?: boolean };
  let { html, class: className = "", lead = false }: Props = $props();
</script>

{#if html}
  <div
    use:glossaryLinks={html}
    class={cn(
      "prose prose-slate max-w-none prose-a:text-blue-700 prose-a:underline-offset-2 prose-code:before:content-none prose-code:after:content-none",
      "[&_.book-ref]:text-blue-700 [&_.book-ref]:underline [&_.book-ref]:underline-offset-2",
      "[&_.book-glossary]:underline [&_.book-glossary]:decoration-slate-600 [&_.book-glossary]:decoration-dotted [&_.book-glossary]:underline-offset-2",
      lead ? "prose-base" : "prose-sm",
      className,
    )}
  >
    {@html html}
  </div>
{/if}
