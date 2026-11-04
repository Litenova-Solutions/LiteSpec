<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Ref from "$lib/components/book/Ref.svelte";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import { codeCls, browserReached } from "../helpers";

  // The screens that offer a use case. The block stays off the page with the
  // other link-only blocks; the rail shows the same links.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const sc = $derived(book.screensOf(id));
  const reached = $derived(browserReached(r));
</script>

{#if sc.length || reached}
  <Block blockKey="where" heading="Points at this: offers" count={sc.length || null}>
    {#if !sc.length}
      {@const rt = (((r.fields as Record<string, unknown>)?.reachedThrough as string[] | undefined) ?? []).map(String)}
      <Callout kind="bad" icon="screen">
        <b>No screen offers this action.</b>{" "}
        It is reached through{" "}
        {#each rt as x, i}{#if i > 0}, {/if}<InlineRef id={x} />{/each}, but no page lets a person start it.
      </Callout>
    {:else}
      {#each sc as x (x)}
        {@const q = book.get(x)}
        <div class="flex flex-wrap items-center gap-2 border-b border-border py-1.5 last:border-b-0">
          <Ref id={x} />
          {#if q}
            <span class={codeCls}>{String(q.fields?.address ?? "")}</span>
            {#if q.route}
              <FactBadge text="implemented" />
            {:else}
              <FactBadge text="not implemented" />
            {/if}
            {#if ((q.calls as string[] | undefined) ?? []).includes(id)}
              <FactBadge text="calls" />
            {:else if q.route}
              <FactBadge text="no calls link" />
            {/if}
          {/if}
        </div>
      {/each}
    {/if}
  </Block>
{/if}
