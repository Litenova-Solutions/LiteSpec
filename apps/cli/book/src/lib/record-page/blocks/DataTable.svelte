<script lang="ts">
  // A bordered table. Cells arrive rendered, each with its own td wrapper.
  type Props = { head: string[]; rows: string[][] };
  let { head, rows }: Props = $props();
</script>

<!-- A table wider than a phone scrolls inside its own frame instead of widening the page. -->
<div class="max-w-full overflow-x-auto rounded-lg border border-border">
<table class="w-full text-sm [overflow-wrap:normal]">
  <thead>
    <tr>
      {#each head as h (h)}
        <th class="border-b border-border bg-muted px-3.5 py-2 text-left text-[0.71875rem] font-medium tracking-[0.06em] text-muted-foreground uppercase">{h}</th>
      {/each}
    </tr>
  </thead>
  <tbody class="[&_tr:last-child_td]:border-b-0">
    {#each rows as r, i (i)}
      <tr>
        {#each r as c, j (j)}{@html c}{/each}
      </tr>
    {/each}
  </tbody>
</table>
</div>
