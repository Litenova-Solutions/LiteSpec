<script lang="ts">
  type Item = {
    label: string;
    value: string;
  };

  type Props = {
    items: Item[];
  };

  let { items }: Props = $props();
</script>

<div class="grid gap-4 sm:grid-cols-2">
  {#each items as item (item.label)}
    <section aria-label={item.label} class="rounded-md border border-border bg-card p-5">
      <p class="text-sm text-muted-foreground">{item.label}</p>
      <p class="text-2xl font-medium tabular-nums">{item.value}</p>
    </section>
  {/each}
</div>
