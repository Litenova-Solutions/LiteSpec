<script lang="ts">
  import { cn } from "$lib/frame";
  import { glossaryLinks } from "./glossary-links";

  // Rendered prose from the CLI or from inline(): the type scale, record links
  // in the link color, code chips, and glossary terms linked on first use.
  // Prose keeps a readable line length whatever the page width.
  type Props = { html: string; class?: string; lead?: boolean };
  let { html, class: className = "", lead = false }: Props = $props();
</script>

{#if html}
  <div
    use:glossaryLinks={html}
    class={cn(
      "prose max-w-[44rem]",
      "[--tw-prose-body:var(--foreground)] [--tw-prose-headings:var(--foreground)] [--tw-prose-lead:var(--foreground)] [--tw-prose-links:var(--link)] [--tw-prose-bold:var(--foreground)] [--tw-prose-counters:var(--text-secondary)] [--tw-prose-bullets:var(--text-secondary)] [--tw-prose-hr:var(--border-subtle)] [--tw-prose-quotes:var(--foreground)] [--tw-prose-quote-borders:var(--border-subtle)] [--tw-prose-captions:var(--text-secondary)] [--tw-prose-code:var(--foreground)] [--tw-prose-pre-code:var(--foreground)] [--tw-prose-pre-bg:var(--layer)] [--tw-prose-th-borders:var(--border-subtle)] [--tw-prose-td-borders:var(--border-subtle)]",
      "[&_a]:text-link [&_a]:no-underline [&_a]:underline-offset-2 [&_a:hover]:underline",
      "[&_.book-glossary]:underline [&_.book-glossary]:decoration-dotted [&_.book-glossary]:decoration-border-strong [&_.book-glossary]:underline-offset-2",
      "[&_code]:rounded-none [&_code]:bg-layer [&_code]:px-1 [&_code]:font-mono [&_code]:text-xs [&_code]:font-normal",
      "prose-code:before:content-none prose-code:after:content-none",
      "[&_h3]:mt-6 [&_h3]:mb-3 [&_h3]:text-sm [&_h3]:leading-6 [&_h3]:font-semibold",
      lead ? "text-xl leading-7 font-light" : "leading-6",
      className,
    )}
  >
    {@html html}
  </div>
{/if}
