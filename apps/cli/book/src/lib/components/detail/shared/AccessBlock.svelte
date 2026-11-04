<script lang="ts">
  import { book, cap } from "$lib/book";
  import type { Fields } from "$lib/book/types";
  import { inline } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import Refs from "../blocks/Refs.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";

  // Who may run a use case: the grant, the tenant scope, and the actors behind it.
  let { id, f }: { id: string; f: Fields } = $props();
  const a = $derived((f.access as Fields | undefined) ?? null);
  const who = $derived(book.performers(id));
  const TEN: Record<string, string> = { own: "own", cross: "cross", public: "public", none: "none" };
</script>

{#if a}
  <Block blockKey="acc" heading="access">
    <Tiles>
      <Tile k="auth">
        {#if ((a.auth as string[] | undefined) ?? []).length}
          {#each (a.auth as string[]) as x, i}{#if i > 0}{" "}{/if}<FactBadge text={x} />{/each}
        {:else}
          none
        {/if}
      </Tile>
      <Tile k="grant">
        {@const g = String(a.grant ?? "")}
        {#if g.indexOf("permission/") === 0}
          <InlineRef id={g} />
        {:else if g.indexOf("self") === 0}
          self{#if g.indexOf("(") > 0}, {@html inline(book, g.slice(g.indexOf("(") + 1, -1), null)}{/if}
        {:else}
          {g}
        {/if}
      </Tile>
      <Tile k="tenant"><FactBadge text={TEN[String(a.tenant)] ?? cap(a.tenant)} /></Tile>
      <Tile k="reachedThrough">
        {@const rt = ((f.reachedThrough as string[] | undefined) ?? [])}
        {#if rt.length}
          {#each rt as x, i}{#if i > 0}, {/if}<InlineRef id={x} />{/each}
        {:else}
          &ndash;
        {/if}
      </Tile>
    </Tiles>
    <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">actor, through roles that allow the grant</div>
    {#if who.length}
      <Refs ids={who.map((w) => w.id)} />
    {:else}
      {@const g = String(a.grant ?? "")}
      <p class="m-0 text-sm text-subtle-foreground">{g === "anonymous" || g === "system" ? "grant: " + g : "No role allows this permission."}</p>
    {/if}
  </Block>
{/if}
