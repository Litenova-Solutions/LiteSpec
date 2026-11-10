<script lang="ts">
  import { book, uniq } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import { esc, inline } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import { glossary } from "$lib/components/book/glossary";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import Ref from "$lib/components/book/Ref.svelte";

  // A layer page: what the layer answers, who writes it, and its
  // record types with the question each answers. The Intent page opens
  // with the Intent coverage, whatever layers the project claims. A layer
  // the project does not claim reads as not claimed, never as missing.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const key = $derived(id.slice(6));
  const unclaimed = $derived(["intent", "behavior", "implementation", "proof"].includes(key) && !book.claimed(key));
  const ids = $derived(book.order.filter((x) => book.layerOf[x] === key && book.isReal(x)));
  const types = $derived(uniq(ids.map((x) => book.typeOf(x))));
  const lead = $derived(f.question ? inline(book, String(f.question), null) : "");
  const coverage = $derived(key === "intent" ? book.intentCoverage() : null);
  const unreached = $derived((coverage?.entries ?? []).filter((entry) => !entry.reached));
  const reached = $derived((coverage?.entries ?? []).filter((entry) => entry.reached));
  const percent = $derived(coverage && coverage.total > 0 ? Math.round((coverage.share ?? coverage.reached / coverage.total) * 100) : 100);
</script>

{#if unclaimed}
  <p class="mb-6 max-w-[72ch] text-sm leading-relaxed text-muted-foreground">
    Not claimed. This project does not claim the {key} layer, so its records are not checked here.
  </p>
{/if}
{#if coverage && coverage.total > 0}
  <Block blockKey="intent-coverage" heading="Intent coverage" count={coverage.total}>
    <p class="mt-0 mb-3 text-sm leading-relaxed">
      {coverage.reached} of {coverage.total} requirements and journey steps reached ({percent}%).
      Requirements: {coverage.requirements.reached} of {coverage.requirements.total}.
      Journey steps: {coverage.steps.reached} of {coverage.steps.total}.
    </p>
    {#if unreached.length}
      <p class="mt-0 mb-1 text-sm font-semibold">Not reached yet</p>
      <ul class="m-0 mb-3 list-none p-0">
        {#each unreached as entry}
          <li class="py-1"><Ref id={entry.id} sub={entry.kind === "step" ? entry.id.slice(entry.id.indexOf("#")) : entry.kind} /></li>
        {/each}
      </ul>
    {/if}
    {#if reached.length}
      <p class="mt-0 mb-1 text-sm font-semibold">Reached</p>
      <ul class="m-0 list-none p-0">
        {#each reached as entry}
          <li class="py-1">
            <Ref id={entry.id} sub={entry.kind === "step" ? entry.id.slice(entry.id.indexOf("#")) : entry.kind} />
            <span class="text-xs text-muted-foreground">reached by {entry.reachedBy.join(", ")}</span>
          </li>
        {/each}
      </ul>
    {/if}
  </Block>
{/if}
{#if lead}
  <p class="mb-6 max-w-[72ch] text-lg leading-relaxed" use:glossary={lead}>{@html lead}</p>
{/if}
{#if f.writes}
  <div class="mb-8">
    <Kv
      pairs={[{ label: "Written by", html: esc(String(f.writes)) }]}
    />
  </div>
{/if}
{#if types.length}
  <Block blockKey="types" heading="Record types" count={types.length}>
    <table class="w-full overflow-hidden rounded-lg border border-border text-sm">
      <thead>
        <tr>
          <th class="border-b border-border bg-muted px-3.5 py-2 text-left text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">
            Type
          </th>
          <th class="border-b border-border bg-muted px-3.5 py-2 text-left text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">
            Records
          </th>
          <th class="border-b border-border bg-muted px-3.5 py-2 text-left text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">
            Question
          </th>
        </tr>
      </thead>
      <tbody class="[&_tr:last-child_td]:border-b-0">
        {#each types as t}
          {@const first = ids.filter((x) => book.typeOf(x) === t)}
          <tr>
            <td class="border-b border-border px-3.5 py-2.5 align-top text-sm">
              <button
                type="button"
                class="underline decoration-strong underline-offset-2 hover:decoration-foreground"
                data-go={first[0]}
              >
                <span class="inline-flex items-center gap-1.5">
                  <Icon name={t} class="size-3.5" />{book.pluralName(t)}
                </span>
              </button>
            </td>
            <td class="border-b border-border px-3.5 py-2.5 align-top text-sm tabular-nums">{first.length}</td>
            <td class="border-b border-border px-3.5 py-2.5 align-top text-sm">{book.typeQuestion(t)}</td>
          </tr>
        {/each}
      </tbody>
    </table>
  </Block>
{/if}
