<script lang="ts">
  import { base, book, type ProofState } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import ProofMark from "$lib/components/book/ProofMark.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import { codeCls, resultInfoOf } from "../helpers";

  // One scenario card: the title with its proof mark, opening to Given, When and Then.
  let { sid, startOpen = false }: { sid: string; startOpen?: boolean } = $props();
  let open = $state<boolean | null>(null);
  const isOpen = $derived(open ?? startOpen);
  const s = $derived(book.get(base(sid)));
  const f = $derived(s?.fields ?? {});
  const ri = $derived(resultInfoOf(sid));
</script>

{#if s}
  <div class="overflow-hidden rounded-lg border border-border">
    <button
      type="button"
      class="flex w-full items-center gap-2.5 bg-background px-3.5 py-2.5 text-left hover:bg-muted"
      aria-expanded={isOpen}
      onclick={() => (open = !isOpen)}
    >
      <Icon name="chev" class="size-3.5 text-muted-foreground transition-transform {isOpen ? 'rotate-90' : ''}" />
      <span class="flex-1 text-sm font-medium">{s.title}</span>
      <ProofMark state={(s.proof?.state as ProofState | undefined) ?? null} />
    </button>
    {#if isOpen}
      <div class="py-1 pr-3.5 pb-3.5 pl-10">
        <dl class="grid grid-cols-[56px_minmax(0,1fr)] gap-x-3 gap-y-1.5 text-sm">
          <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">Given</dt>
          <dd class="m-0">{String(f.given ?? "")}</dd>
          <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">When</dt>
          <dd class="m-0">{String(f.when ?? "")}</dd>
          <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">Then</dt>
          <dd class="m-0">{String(f.then ?? "")}</dd>
        </dl>
        <div class="mt-3 flex flex-wrap items-center gap-2 border-t border-dashed border-border pt-2.5 text-xs text-muted-foreground">
          {#if ri}
            <FactBadge text={ri.title} />
            {#if ri.code}<span class={codeCls}>{ri.code}</span>{/if}
          {/if}
          <span class="flex-1"></span>
          <button type="button" class="underline decoration-strong underline-offset-2 hover:decoration-foreground" data-go={sid}>
            Open scenario
          </button>
        </div>
      </div>
    {/if}
  </div>
{/if}
