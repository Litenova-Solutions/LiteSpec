<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import { ProofMark, RecordIcon } from "$lib/record";
  import Fact from "../blocks/Fact.svelte";
  import GivenWhenThen from "./GivenWhenThen.svelte";
  import { codeCls } from "../page-html";
  import { resultInfoOf, scenarioFields } from "../page-values";

  // One scenario card: the title with its proof mark, opening to Given, When and Then.
  type Props = { sid: string; initiallyOpen?: boolean };
  let { sid, initiallyOpen = false }: Props = $props();
  let open = $state<boolean | null>(null);
  const isOpen = $derived(open ?? initiallyOpen);
  const s = $derived(book.get(stripStep(sid)));
  const cf = $derived(scenarioFields(s?.fields ?? {}));
  const resultId = $derived(book.outs(stripStep(sid), "result")[0] ?? null);
  const ri = $derived(resultInfoOf(resultId ? book.get(resultId) : null));
</script>

{#if s}
  <div class="overflow-hidden rounded-lg border border-slate-300 bg-white shadow-sm">
    <button
      type="button"
      class="flex w-full items-center gap-2.5 bg-white px-3.5 py-2.5 text-left hover:bg-slate-100"
      aria-expanded={isOpen}
      onclick={() => (open = !isOpen)}
    >
      <RecordIcon name="chev" class="size-3.5 text-slate-600 motion-safe:transition-transform {isOpen ? 'rotate-90' : ''}" />
      <span class="flex-1 text-sm font-medium">{s.title}</span>
      <ProofMark state={s.proof?.state ?? null} />
    </button>
    {#if isOpen}
      <div class="py-1 pr-3.5 pb-3.5 pl-10">
        <GivenWhenThen given={cf.given ?? ""} when={cf.when ?? ""} then={cf.then ?? ""} />
        <div class="mt-3 flex flex-wrap items-center gap-2 border-t border-dashed border-slate-200 pt-2.5 text-xs text-slate-600">
          {#if ri}
            <Fact text={ri.title} />
            {#if ri.code}<span class={codeCls}>{ri.code}</span>{/if}
          {/if}
          <span class="flex-1"></span>
          <button type="button" class="underline decoration-slate-300 underline-offset-2 hover:decoration-slate-900" data-go={sid}>
            Open scenario
          </button>
        </div>
      </div>
    {/if}
  </div>
{/if}
