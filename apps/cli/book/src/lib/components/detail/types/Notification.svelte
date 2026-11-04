<script lang="ts">
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Table from "../blocks/Table.svelte";
  import { codeCls, proseOf, tdHtml } from "../helpers";

  // A notification: its parameters, and the claims each message makes.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const arr = (v: unknown) => (Array.isArray(v) ? v : v ? [v] : []) as Fields[];
  const params = $derived(arr(f.params));
  const claims = $derived(arr(f.claims));
</script>

{#if whatHappens}
  <Block blockKey="wh" heading="whatHappens"><Prose html={whatHappens} /></Block>
{/if}
{#if params.length}
  <Block blockKey="params" heading="params" count={params.length}>
    {#if typeof params[0] === "string"}
      <div class="flex flex-wrap gap-1">
        {#each params as p}<span class={codeCls}>{String(p)}</span>{/each}
      </div>
    {:else}
      <Table
        head={["Parameter", "Meaning", "Source"]}
        rows={params.map((p) => [
          tdHtml(esc(String(p.name ?? "")), "nm"),
          tdHtml(esc(String(p.meaning ?? ""))),
          tdHtml(esc(String(p.source ?? "")), "mut"),
        ])}
      />
    {/if}
  </Block>
{/if}
{#if claims.length}
  <Block blockKey="claims" heading="claims" count={claims.length}>
    {#each claims as c}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <Icon name="quote" class="size-3.5" />{String(c.claim ?? "")}
        </div>
        {#if c.requirement}
          <p class="mt-1 mb-0 text-sm text-subtle-foreground">Backed by <InlineRef id={String(c.requirement)} /></p>
        {:else}
          <p class="mt-1 mb-0 text-xs font-medium text-warn-foreground">No requirement backs this claim</p>
        {/if}
      </div>
    {/each}
  </Block>
{/if}
