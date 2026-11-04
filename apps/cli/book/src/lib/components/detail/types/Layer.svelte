<script lang="ts">
  import { book, uniq } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import { esc, inline } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import { glossary } from "$lib/components/book/glossary";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";

  // A layer page: what the layer answers, who writes it, and its
  // record types with the question each answers.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const key = $derived(id.slice(6));
  const ids = $derived(book.order.filter((x) => book.layerOf[x] === key && book.isReal(x)));
  const types = $derived(uniq(ids.map((x) => book.typeOf(x))));
  const lead = $derived(f.question ? inline(book, String(f.question), null) : "");
</script>

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
