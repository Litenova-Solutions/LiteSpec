<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { objectList, stringField } from "../page-values";

  // A domain event: the fields it carries, each with its type.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const fields = $derived(objectList(r.fields ?? {}, "fields"));
</script>

{#if fields.length}
  <PageSection blockKey="carry" heading="fields" count={fields.length}>
    <ul class="grid gap-1">
      {#each fields as c, i (i)}
        <li>
          <code class="bg-layer px-1 font-mono text-xs">{stringField(c, "name") ?? ""}</code>: {stringField(c, "type") ?? ""}
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
