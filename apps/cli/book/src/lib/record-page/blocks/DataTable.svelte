<script lang="ts">
  // A bordered table. Cells arrive rendered, each with its own td wrapper.
  type Props = { head: string[]; rows: string[][]; caption?: string | null };
  let { head, rows, caption = null }: Props = $props();
  const uid = $props.id();
  const captionId = $derived(uid + "-cap");
</script>

<!-- A table wider than its frame scrolls inside the frame instead of widening the page. -->
<!-- The scroll region takes keyboard focus so a keyboard user can scroll a wide table. -->
<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
<div class="max-w-full overflow-x-auto" tabindex="0" role="region" aria-labelledby={caption ? captionId : null}>
<table class="w-full text-sm [overflow-wrap:normal]">
  {#if caption}<caption class="sr-only" id={captionId}>{caption}</caption>{/if}
  <thead>
    <tr>
      {#each head as h (h)}
        <th scope="col" class="bg-layer-accent h-10 px-4 text-left font-semibold">{h}</th>
      {/each}
    </tr>
  </thead>
  <tbody class="[&_tr:hover_td]:bg-layer-hover [&_tr:hover_th]:bg-layer-hover">
    {#each rows as r, i (i)}
      <tr>
        {#each r as c, j (j)}{@html c}{/each}
      </tr>
    {/each}
  </tbody>
</table>
</div>
