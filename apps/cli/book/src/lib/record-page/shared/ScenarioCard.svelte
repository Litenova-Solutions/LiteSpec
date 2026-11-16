<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import { ProofMark, RecordIcon, RecordLink } from "$lib/record";
  import GivenWhenThen from "./GivenWhenThen.svelte";
  import { resultInfoOf, scenarioFields } from "../page-values";
  import { tag } from "./record-cells";

  // One scenario in the accordion: the title with its result and proof tags,
  // opening to Given, When and Then.
  type Props = { sid: string; initiallyOpen?: boolean; group?: string };
  let { sid, initiallyOpen = false, group = "scenarios" }: Props = $props();
  // The first render opens a card the block asks to open; the reader toggles it after.
  // svelte-ignore state_referenced_locally
  let open = $state(initiallyOpen);
  const s = $derived(book.get(stripStep(sid)));
  const cf = $derived(scenarioFields(s?.fields ?? {}));
  const resultId = $derived(book.outs(stripStep(sid), "result")[0] ?? null);
  const ri = $derived(resultInfoOf(resultId ? book.get(resultId) : null));
</script>

{#if s}
  <details
    name={group}
    {open}
    class="border-b border-border-subtle"
    ontoggle={(e) => {
      open = (e.currentTarget as HTMLDetailsElement).open;
    }}
  >
    <summary class="flex min-h-10 cursor-pointer items-center gap-3 px-2 hover:bg-layer-hover">
      <RecordIcon name="chev" class="size-4 text-text-secondary motion-safe:transition-transform {open ? 'rotate-90' : ''}" />
      <span class="flex-1">{s.title}</span>
      {#if ri}{@html tag(ri.title, ri.outcome === "success" ? "green" : "red", "sm", "g:result")}{/if}
      <ProofMark state={s.proof?.state ?? null} />
    </summary>
    <div class="pb-6 pl-9">
      <GivenWhenThen given={cf.given ?? ""} when={cf.when ?? ""} then={cf.then ?? ""} />
      {#if cf.exercises.length}
        <p class="mt-4 text-sm text-text-secondary">
          {book.fieldLabel("exercises")}
          {#each cf.exercises as x, i (x)}{#if i > 0}{", "}{/if}<RecordLink id={x} />{/each}
        </p>
      {/if}
    </div>
  </details>
{/if}
