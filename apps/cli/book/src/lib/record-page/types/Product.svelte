<script lang="ts">
  import { preferences } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import PageSection from "../blocks/PageSection.svelte";
  import { stringList } from "../page-values";

  // The product page: the intro for new readers, and what the product excludes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const exclusions = $derived(stringList(r.fields ?? {}, "exclusions"));
  const showIntro = $derived(!preferences.introDismissed);
</script>

{#if showIntro}
  <div class="mb-[22px] grid grid-cols-[20px_minmax(0,1fr)_auto] items-start gap-3 border-l-[3px] border-highlight py-0.5 pl-3.5 text-sm">
    <RecordIcon name="book" class="size-4" />
    <div>
      <b>New here?</b> The outline follows the four record layers. The rail on the right traces each record from its goal down to the code and tests. Hover
      any mark, type or link for its LiteSpec definition. Every source path opens in the editor.
    </div>
    <Button variant="ghost" size="icon-sm" onclick={() => preferences.dismissIntro()} aria-label="Close this"><RecordIcon name="x" class="size-3.5" /></Button>
  </div>
{/if}
{#if exclusions.length}
  <PageSection blockKey="exc" heading="exclusions" count={exclusions.length}>
    <ul class="m-0 list-none border-t border-border p-0">
      {#each exclusions as x (x)}
        <li class="grid grid-cols-[20px_1fr] gap-2 border-b border-border py-2.5 text-sm">
          <RecordIcon name="x" class="mt-1 size-3.5 text-warn-foreground" />
          <span>{x}</span>
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
