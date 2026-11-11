<script lang="ts">
  // A bordered table. Cells arrive rendered, each with its own td wrapper.
  let { head, rows }: { head: string[]; rows: string[][] } = $props();
</script>

<table class="w-full overflow-hidden rounded-lg border border-border text-sm">
  <thead>
    <tr>
      {#each head as h}
        <th class="border-b border-border bg-muted px-3.5 py-2 text-left text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">{h}</th>
      {/each}
    </tr>
  </thead>
  <tbody class="[&_tr:last-child_td]:border-b-0">
    {#each rows as r}
      <tr>
        {#each r as c}{@html c}{/each}
      </tr>
    {/each}
  </tbody>
</table>
