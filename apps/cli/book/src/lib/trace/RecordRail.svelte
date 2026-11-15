<script lang="ts">
  import { book } from "$lib/frame";
  import CompletionSection from "./CompletionSection.svelte";
  import PageContents from "./PageContents.svelte";
  import TraceSection from "./TraceSection.svelte";

  // The rail beside the record page: the cross-layer trace, the record's
  // completion, and the sections of its page. Each section reads only when
  // it has anything to say; the record page lists its links and findings,
  // so the rail repeats neither.
  const id = $derived(book.selected);
</script>

<TraceSection {id} />
<CompletionSection {id} />
<PageContents />
