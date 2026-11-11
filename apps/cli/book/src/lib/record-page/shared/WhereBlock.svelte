<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLine, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { codeCls } from "../page-html";
  import { browserReached, stringField, stringList } from "../page-values";

  // The screens that offer a use case. The block stays off the page with the
  // other link-only blocks; the rail shows the same links.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const sc = $derived(book.screensOf(id));
  const reachedThrough = $derived(stringList(r.fields ?? {}, "reachedThrough"));
  const reached = $derived(browserReached(reachedThrough, (e) => book.kids(e).some((k) => book.typeOf(k) === "screen")));
</script>

{#if sc.length || reached}
  <PageSection blockKey="where" heading="Points at this: offers" count={sc.length || null}>
    {#if !sc.length}
      <Note kind="bad" icon="screen">
        <b>No screen offers this action.</b>{" "}
        It is reached through{" "}
        {#each reachedThrough as x, i (x)}{#if i > 0}, {/if}<RecordLink id={x} />{/each}, but no page lets a person start it.
      </Note>
    {:else}
      {#each sc as x (x)}
        {@const q = book.get(x)}
        <div class="flex flex-wrap items-center gap-2 border-b border-border py-1.5 last:border-b-0">
          <RecordLine id={x} />
          {#if q}
            <span class={codeCls}>{String(stringField(q.fields ?? {}, "address") ?? "")}</span>
            {#if q.route}
              <Fact text="coded" />
            {:else}
              <Fact text="not coded" />
            {/if}
            {#if (q.calls ?? []).includes(id)}
              <Fact text="calls" />
            {:else if q.route}
              <Fact text="no calls link" />
            {/if}
          {/if}
        </div>
      {/each}
    {/if}
  </PageSection>
{/if}
