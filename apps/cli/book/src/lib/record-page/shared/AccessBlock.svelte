<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { objectField, stringField, stringList, tenantWord } from "../page-values";

  // Who may run a use case: the grant, the tenant scope, and the actors behind it.
  type Props = { id: string; f: RecordFields };
  let { id, f }: Props = $props();
  const a = $derived(objectField(f, "access"));
  const who = $derived(book.performers(id));
  const auth = $derived(stringList(a, "authentication"));
  const grant = $derived(a ? (stringField(a, "grant") ?? "") : "");
  const selfGrant = $derived(grant.startsWith("self(") && grant.endsWith(")") ? grant.slice(5, -1) : "");
  const via = $derived(stringList(f, "via"));
  const tenant = $derived(tenantWord(a?.tenant));
</script>

{#if a}
  <PageSection blockKey="acc" heading="access">
    <DefinitionList>
      {#if auth.length}
        <DefinitionRow label="authentication">
          {#each auth as x, i (x)}{#if i > 0}{", "}{/if}<code class="bg-layer px-1 font-mono text-xs">{x}</code>{/each}
        </DefinitionRow>
      {/if}
      <DefinitionRow label="Grant">
        {#if grant.indexOf("permission/") === 0}
          <RecordLink id={grant} />
        {:else if selfGrant}
          <code class="bg-layer px-1 font-mono text-xs">self</code> on <RecordLink id={selfGrant} />
        {:else}
          {grant}
        {/if}
      </DefinitionRow>
      <DefinitionRow label="Tenant">{capitalize(tenant)}</DefinitionRow>
      {#if via.length}
        <DefinitionRow label="Reached through">
          {#each via as x, i (x)}{#if i > 0}{", "}{/if}<RecordLink id={x} />{/each}
        </DefinitionRow>
      {/if}
      <DefinitionRow label="Roles">
        {#if who.length}
          <RecordList ids={who.map((w) => w.id)} />
        {:else if grant === "anonymous" || grant === "system"}
          grant: {grant}
        {:else}
          No role grants this permission.
          {#if grant.indexOf("self") === 0}
            <span class="mt-1 block text-xs tracking-[0.32px] text-text-helper"
              >The grant is on the caller's own person, so no role table applies.</span
            >
          {/if}
        {/if}
      </DefinitionRow>
    </DefinitionList>
  </PageSection>
{/if}
