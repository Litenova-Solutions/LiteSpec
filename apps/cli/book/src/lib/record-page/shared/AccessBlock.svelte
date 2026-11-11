<script lang="ts">
  import { book, inline } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { objectField, stringField, stringList, tenantWord } from "../page-values";

  // Who may run a use case: the grant, the tenant scope, and the actors behind it.
  type Props = { id: string; f: RecordFields };
  let { id, f }: Props = $props();
  const a = $derived(objectField(f, "access"));
  const who = $derived(book.performers(id));
  const auth = $derived(stringList(a, "auth"));
  const grant = $derived(a ? (stringField(a, "grant") ?? "") : "");
  const reachedThrough = $derived(stringList(f, "reachedThrough"));
  const tenant = $derived(tenantWord(a?.tenant));
</script>

{#if a}
  <PageSection blockKey="acc" heading="access">
    <FactGrid>
      <FactCell k="auth">
        {#if auth.length}
          {#each auth as x, i (i)}{#if i > 0}{" "}{/if}<Fact text={x} />{/each}
        {:else}
          none
        {/if}
      </FactCell>
      <FactCell k="grant">
        {#if grant.indexOf("permission/") === 0}
          <RecordLink id={grant} />
        {:else if grant.indexOf("self") === 0}
          self{#if grant.indexOf("(") > 0}, {@html inline(book, grant.slice(grant.indexOf("(") + 1, -1), null)}{/if}
        {:else}
          {grant}
        {/if}
      </FactCell>
      <FactCell k="tenant"><Fact text={tenant} /></FactCell>
      <FactCell k="reachedThrough">
        {#if reachedThrough.length}
          {#each reachedThrough as x, i (x)}{#if i > 0}, {/if}<RecordLink id={x} />{/each}
        {:else}
          &ndash;
        {/if}
      </FactCell>
    </FactGrid>
    <div class="mt-[18px] mb-2 text-[0.8125rem] font-semibold text-subtle-foreground">actor, through roles that allow the grant</div>
    {#if who.length}
      <RecordList ids={who.map((w) => w.id)} />
    {:else}
      <p class="m-0 text-sm text-subtle-foreground">{grant === "anonymous" || grant === "system" ? "grant: " + grant : "No role allows this permission."}</p>
    {/if}
  </PageSection>
{/if}
