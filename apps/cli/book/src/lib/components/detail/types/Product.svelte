<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Button } from "$lib/components/ui/button";
  import Block from "../blocks/Block.svelte";

  // The product page: the intro for new readers, and what the product excludes.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const exclusions = $derived(((r.fields?.exclusions as string[] | undefined) ?? []).map(String));

  function introClosed() {
    try {
      return localStorage.getItem("book-intro") === "closed";
    } catch {
      return false;
    }
  }
  let dismissed = $state(false);
  const showIntro = $derived(!dismissed && !introClosed());

  function close() {
    try {
      localStorage.setItem("book-intro", "closed");
    } catch {
      // The store is unavailable; the banner still closes for this view.
    }
    dismissed = true;
  }
</script>

{#if showIntro}
  <div class="mb-[22px] grid grid-cols-[20px_minmax(0,1fr)_auto] items-start gap-3 border-l-[3px] border-highlight py-0.5 pl-3.5 text-sm">
    <Icon name="book" class="size-4" />
    <div>
      <b>New here?</b> The outline follows the four record layers. The rail on the right traces each record from its goal down to the code and tests. Hover
      any mark, type or link for its LiteSpec definition. Every source path opens in the editor.
    </div>
    <Button variant="ghost" size="icon-sm" onclick={close} aria-label="Close this"><Icon name="x" class="size-3.5" /></Button>
  </div>
{/if}
{#if exclusions.length}
  <Block blockKey="exc" heading="exclusions" count={exclusions.length}>
    <ul class="m-0 list-none border-t border-border p-0">
      {#each exclusions as x}
        <li class="grid grid-cols-[20px_1fr] gap-2 border-b border-border py-2.5 text-sm">
          <Icon name="x" class="mt-1 size-3.5 text-warn-foreground" />
          <span>{x}</span>
        </li>
      {/each}
    </ul>
  </Block>
{/if}
