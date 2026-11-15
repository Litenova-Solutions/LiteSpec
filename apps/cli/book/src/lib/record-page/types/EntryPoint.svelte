<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import NavigationMap from "../shared/NavigationMap.svelte";
  import { screenFields, stringField, stringList } from "../page-values";

  // An entry point: the path it serves, and the navigation map of its screens.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const path = $derived(stringField(f, "path"));
  // The screens of this entry point in outline order: the screens placed
  // below it, and the screens naming it as their entry point.
  const screens = $derived.by(() => {
    const kids = new Set(book.kids(id));
    return book.order.filter((x) => {
      if (book.typeOf(x) !== "screen") return false;
      if (kids.has(x)) return true;
      return stripStep(stringField(book.get(x)?.fields, "entryPoint") ?? "") === id;
    });
  });
  const nodes = $derived(screens.map((x) => ({ id: x, title: book.title(x) })));
  const leads = $derived.by(() => {
    const out: Record<string, string[]> = {};
    for (const x of screens) out[x] = stringList(book.get(x)?.fields, "leadsTo").map((t) => stripStep(t));
    return out;
  });
  // The start screen is the one at the root address; the map marks it, and
  // shows no marker until the data carries that screen.
  const start = $derived.by(() => {
    for (const x of screens) {
      if (screenFields(book.get(x)?.fields ?? {}).address === "/") return x;
    }
    return null;
  });
</script>

{#if path}
  <PageSection blockKey="path" heading="path">
    <span class="bg-layer px-2 py-1 font-mono text-sm">{path}</span>
  </PageSection>
{/if}
<PageSection blockKey="map" heading="Navigation map" count={screens.length > 0 ? screens.length : null}>
  {#if screens.length === 0}
    <p class="text-sm text-text-secondary">No screens.</p>
  {:else}
    <NavigationMap screens={nodes} {leads} {start} />
  {/if}
</PageSection>
