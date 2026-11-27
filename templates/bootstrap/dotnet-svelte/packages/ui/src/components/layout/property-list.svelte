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

<dl class="grid gap-2">
  {#each items as item (item.label)}
    <div class="grid gap-1">
      <dt class="text-sm text-muted-foreground">{item.label}</dt>
      <dd class="text-sm tabular-nums">{item.value}</dd>
    </div>
  {/each}
</dl>
