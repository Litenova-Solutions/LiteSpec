<script lang="ts">
  import { book } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";

  // The records below a grouping: children in the outline, 150 with a Show all button.
  let { id }: { id: string } = $props();
  const kids = $derived(book.kids(id));
</script>

{#if kids.length}
  <PageSection blockKey="in" heading="Records" count={kids.length}>
    <RecordList ids={kids} limit={150} moreKey={id + "|records"} />
  </PageSection>
{/if}
