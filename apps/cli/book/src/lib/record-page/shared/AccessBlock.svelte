<script lang="ts">
  import { book, capitalize, inline } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";

  // Who may run a use case: the grant, the tenant scope, and the actors behind it.
  let { id, f }: { id: string; f: RecordFields } = $props();
  const a = $derived((f.access as RecordFields | undefined) ?? null);
  const who = $derived(book.performers(id));
  const TEN: Record<string, string> = { own: "own", cross: "cross", public: "public", none: "none" };
</script>

{#if a}
  <PageSection blockKey="acc" heading="access">
    <FactGrid>
      <FactCell k="auth">
        {#if ((a.auth as string[] | undefined) ?? []).length}
          {#each (a.auth as string[]) as x, i}{#if i > 0}{" "}{/if}<Fact text={x} />{/each}
        {:else}
          none
        {/if}
      </FactCell>
      <FactCell k="grant">
        {@const g = String(a.grant ?? "")}
        {#if g.indexOf("permission/") === 0}
          <RecordLink id={g} />
        {:else if g.indexOf("self") === 0}
          self{#if g.indexOf("(") > 0}, {@html inline(book, g.slice(g.indexOf("(") + 1, -1), null)}{/if}
        {:else}
          {g}
        {/if}
      </FactCell>
      <FactCell k="tenant"><Fact text={TEN[String(a.tenant)] ?? capitalize(a.tenant)} /></FactCell>
      <FactCell k="reachedThrough">
        {@const rt = ((f.reachedThrough as string[] | undefined) ?? [])}
        {#if rt.length}
          {#each rt as x, i}{#if i > 0}, {/if}<RecordLink id={x} />{/each}
        {:else}
          &ndash;
        {/if}
      </FactCell>
    </FactGrid>
    <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">actor, through roles that allow the grant</div>
    {#if who.length}
      <RecordList ids={who.map((w) => w.id)} />
    {:else}
      {@const g = String(a.grant ?? "")}
      <p class="m-0 text-sm text-subtle-foreground">{g === "anonymous" || g === "system" ? "grant: " + g : "No role allows this permission."}</p>
    {/if}
  </PageSection>
{/if}
