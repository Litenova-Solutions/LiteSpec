<script lang="ts">
  import { book, type View } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import * as Tabs from "$lib/components/ui/tabs";
  import { ballotItems, securityItems } from "./rows";

  // The four navigation views as a segmented control: Outline, Review queue,
  // Security and Ballots, the last two with their counts.
  let { onSwitch }: { onSwitch: (v: View) => void } = $props();
  const counts = $derived.by(() => {
    void book.rev;
    // A count shows once the queue holds it, never a guess from the records loaded so far.
    if (!book.queue) return { security: null, ballots: null };
    return {
      security: securityItems(book).length,
      ballots: ballotItems(book).filter((x) => !book.answered[x.id + "#" + x.i]).length,
    };
  });
  const tabs = [
    { value: "outline", label: "Outline", icon: "tree" },
    { value: "queue", label: "Review queue", icon: "arrow" },
    { value: "security", label: "Security", icon: "shield" },
    { value: "ballots", label: "Ballots", icon: "ballot" },
  ] as const;
</script>

<Tabs.Root value={book.view} activationMode="manual" onValueChange={(v) => onSwitch(v as View)}>
  <Tabs.List class="grid w-full grid-cols-2">
    {#each tabs as t, i (t.value)}
      <Tabs.Trigger value={t.value} title="{t.label} ({i + 1})" class="data-[state=active]:bg-background data-[state=active]:shadow-sm data-[state=active]:font-medium">
        <Icon name={t.icon} class="size-3.5" />
        {t.label}
        {#if t.value === "security" && counts.security !== null}
          <span class="font-mono text-[10.5px] text-muted-foreground">{counts.security}</span>
        {/if}
        {#if t.value === "ballots" && counts.ballots !== null}
          <span class="font-mono text-[10.5px] text-muted-foreground">{counts.ballots}</span>
        {/if}
      </Tabs.Trigger>
    {/each}
  </Tabs.List>
</Tabs.Root>
