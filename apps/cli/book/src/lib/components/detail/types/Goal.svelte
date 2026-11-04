<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import { proseOf } from "../helpers";

  // A goal: why it matters, and the measure with its target and due date.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const why = $derived(proseOf(r, "whyItMatters"));
</script>

{#if why}
  <Block blockKey="wim" heading="whyItMatters"><Prose html={why} /></Block>
{/if}
<Block blockKey="m" heading="measure">
  <Tiles>
    <Tile k="Measure">{String(f.measure ?? "")}</Tile>
    <Tile k="target" big>{String(f.target ?? "")}</Tile>
    <Tile k="dueBy" hint={f.dueBy ? book.dateHint(String(f.dueBy)) : ""}>
      {f.dueBy ? book.fmtDate(String(f.dueBy)) : ""}
    </Tile>
  </Tiles>
</Block>
