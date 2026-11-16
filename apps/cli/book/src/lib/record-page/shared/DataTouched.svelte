<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import { hoverRelation } from "$lib/frame/hover-text";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import Hover from "$lib/record/Hover.svelte";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import { statementOf } from "../page-prose";
  import { idemTileOf, objectField, objectList, stringField, stringList } from "../page-values";
  import { movesOf } from "../types/use-case-model";
  import { idemHint } from "./record-cells";

  // The data a behavior touches: what it writes, what it reads, and how the two
  // agree. A use case also states the events it raises and its idempotency and
  // audit here.
  type Props = { id?: string | null; f: RecordFields; guarantees?: boolean; carrier?: string | null };
  let { id = null, f, guarantees = false, carrier = null }: Props = $props();
  const writes = $derived(stringList(f, "writes"));
  const reads = $derived(stringList(f, "reads"));
  const kind = $derived(stringField(f, "kind"));
  const raises = $derived(stringList(f, "raises"));
  const showRaises = $derived(kind !== "query" && (raises.length > 0 || (kind === "command" && writes.length > 0)));
  const moves = $derived.by(() => {
    if (!id || !raises.length) return [];
    return movesOf(
      id,
      writes.map((w) => ({
        id: w,
        title: book.title(w),
        transitions: objectList(book.get(w)?.fields, "transitions"),
      })),
    );
  });
  const consistency = $derived(objectField(f, "consistency"));
  const boundBy = $derived(stringList(consistency, "boundBy"));
  const guards = $derived(stringList(consistency, "guards"));
  const guardsPhrase = $derived(book.phraseFor("consistency.guards", carrier, "forward"));
  const idem = $derived(guarantees ? idemTileOf(f) : null);
  const idemHintHtml = $derived.by(() => {
    const i = guarantees ? objectField(f, "idempotency") : undefined;
    return i ? idemHint(i) : "";
  });
  const audit = $derived(guarantees ? objectField(f, "audit") : undefined);
  const exemptBecause = $derived(stringField(audit, "exemptBecause"));
  const auditLevel = $derived(stringField(audit, "level"));
  const auditTarget = $derived(stringField(audit, "target"));
  const writesStatement = $derived.by(() => {
    const first = writes[0];
    if (!first) return "";
    return statementOf(book.get(first)?.fields ?? {}) ?? "";
  });
  const has = $derived(writes.length > 0 || reads.length > 0 || !!consistency || !!idem || !!audit || showRaises);

  function raiseStatement(e: string): string {
    return statementOf(book.get(e)?.fields ?? {}) ?? "";
  }
</script>

{#if has}
  <PageSection blockKey="data" heading="data">
    <DefinitionList>
      {#if writes.length}
        <DefinitionRow label="Writes">
          {#each writes as w, i (w)}{#if i > 0}{", "}{/if}<RecordLink id={w} />{/each}
          {#if writesStatement}<span class="mt-1 block text-xs tracking-[0.32px] text-text-helper">{writesStatement}</span>{/if}
        </DefinitionRow>
      {/if}
      {#if showRaises}
        <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">
          <Hover hover={hoverRelation(book, "raises", "out", id ?? "")}>
            <span class="cursor-help">{book.fieldLabel("Raises")}</span>
          </Hover>
        </dt>
        <dd class="m-0 min-w-0 border-b border-border-subtle py-3">
          {#if raises.length}
            <ul>
              {#each raises as e (e)}
                <li class="mt-3 first:mt-0">
                  <RecordLink id={e} />
                  {#if raiseStatement(e)}<span class="mt-1 block text-xs tracking-[0.32px] text-text-helper">{raiseStatement(e)}</span>{/if}
                </li>
              {/each}
            </ul>
            {#each moves as m (m.aggregate + "|" + m.to)}
              <span class="mt-2 block">Moves {m.aggregate} to {m.to}</span>
            {/each}
          {:else}
            no domain event
          {/if}
        </dd>
      {/if}
      {#if reads.length}
        <DefinitionRow label="Reads">
          {#each reads as x, i (x)}{#if i > 0}{", "}{/if}<RecordLink id={x} />{/each}
        </DefinitionRow>
      {/if}
      {#if consistency}
        <DefinitionRow label="Consistency">
          {capitalize(consistency.mode)}
          {#if boundBy.length}
            <span class="mt-1 block font-mono text-xs text-text-helper">
              because {#each boundBy as c, i (c)}{#if i > 0}{", "}{/if}<RecordLink id={c} />{/each}
            </span>
          {/if}
          {#if guards.length}
            <span class="mt-1 block font-mono text-xs text-text-helper">
              {guardsPhrase} {#each guards as g, i (g)}{#if i > 0}{", "}{/if}<RecordLink id={g} />{/each}
            </span>
          {/if}
        </DefinitionRow>
      {/if}
      {#if idem}
        <DefinitionRow label="Idempotency">
          {@html idem.value}
          {#if idemHintHtml}<span class="mt-1 block text-xs tracking-[0.32px] text-text-helper">{@html idemHintHtml}</span>{/if}
        </DefinitionRow>
      {/if}
      {#if audit}
        <DefinitionRow label="Audit">
          {#if exemptBecause}
            Not audited. {exemptBecause}
          {:else}
            {auditLevel ? capitalize(auditLevel) + "." : ""}{auditTarget ? ` ${auditTarget}` : ""}
          {/if}
        </DefinitionRow>
      {/if}
    </DefinitionList>
  </PageSection>
{/if}
