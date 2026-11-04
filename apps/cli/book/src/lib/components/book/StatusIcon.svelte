<script lang="ts">
  import { book, HLABEL } from "$lib/book";
  import { cn } from "$lib/utils";
  import Icon from "./Icon.svelte";

  // The review status as colour plus icon shape, so it never rests on colour alone.
  let { id, class: className = "" }: { id: string; class?: string } = $props();
  const ICON = {
    approved: "approved", reviewed: "approved", mine: "mine", mineR: "mine", draft: "draft",
    inherited: "inherited", question: "question", changes: "changes", retired: "refused",
  } as const;
  const h = $derived(book.human(id));
  const tone = $derived(
    h === "draft" || h === "question" || h === "changes"
      ? "text-amber-600 dark:text-amber-400"
      : h === "inherited" || h === "retired"
        ? "text-muted-foreground"
        : "text-green-600 dark:text-green-400",
  );
</script>

{#if book.isReal(id)}
  <span class={cn("inline-flex", tone, className)} title={HLABEL[h]}><Icon name={ICON[h]} class="size-3.5" /></span>
{/if}
