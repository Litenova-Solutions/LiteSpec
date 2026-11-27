<script lang="ts">
  import { book, copyText, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import ScenarioCard from "../shared/ScenarioCard.svelte";
  import { isRecord, refusalCodeOf, stringField } from "../page-values";

  // A result: what it means, its code, its problem type, and the scenarios that show it.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const code = $derived(refusalCodeOf(stringField(f, "refusal") ?? stringField(f, "code") ?? "") || null);
  const problemType = $derived(stringField(f, "problemType"));
  const message = $derived(stringField(f, "message"));
  const translations = $derived.by(() => {
    const v = f.translations;
    if (!isRecord(v)) return [];
    return Object.keys(v).map((lang) => ({ lang, text: String(v[lang] ?? "") })).filter((x) => x.text !== "");
  });
  const sc = $derived(book.shownByOf(id).filter((x) => book.get(stripStep(x))));
</script>

{#if code}
  <PageSection blockKey="code" heading="Refusal code">
    <span class="inline-flex min-h-6 items-center gap-2 bg-layer pl-2 font-mono text-xs [overflow-wrap:anywhere]"
      >{code}<button
        type="button"
        class="flex size-6 items-center justify-center hover:bg-hover-wash"
        aria-label="Copy code"
        onclick={() => copyText(code)}
      >
        <RecordIcon name="copy" class="size-4" />
      </button></span
    >
  </PageSection>
{/if}
{#if problemType}
  <PageSection blockKey="ptype" heading="problemType">
    <span class="inline-flex min-h-6 items-center gap-2 bg-layer pl-2 font-mono text-xs [overflow-wrap:anywhere]"
      >{problemType}<button
        type="button"
        class="flex size-6 items-center justify-center hover:bg-hover-wash"
        aria-label="Copy problem type"
        onclick={() => copyText(problemType)}
      >
        <RecordIcon name="copy" class="size-4" />
      </button></span
    >
  </PageSection>
{/if}
{#if message || translations.length}
  <PageSection blockKey="sent" heading="Message">
    <DefinitionList>
      {#if message}<DefinitionRow label="Message">{message}</DefinitionRow>{/if}
      {#each translations as s (s.lang)}
        <DefinitionRow label={s.lang}>{s.text}</DefinitionRow>
      {/each}
    </DefinitionList>
  </PageSection>
{/if}
{#if sc.length}
  <PageSection blockKey="ex" heading="Scenarios" count={sc.length}>
    <div class="border-t border-border-subtle" role="group" aria-label={book.fieldLabel("Scenarios")}>
      {#each sc as x (x)}
        <ScenarioCard sid={x} group={id} />
      {/each}
    </div>
  </PageSection>
{/if}
