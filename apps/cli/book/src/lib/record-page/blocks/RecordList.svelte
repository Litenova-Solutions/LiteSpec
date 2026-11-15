<script lang="ts">
  import { book, dedupe, stripStep } from "$lib/frame";
  import { TypeTag } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import { statementOf } from "../page-prose";

  // Records as a numbered list: the title link, the type label and the
  // expandable statement. Long lists keep a limit with a Show all button.
  type Props = {
    ids: (string | null | undefined)[];
    limit?: number | null;
    moreKey?: string;
  };
  let { ids, limit = null, moreKey = "" }: Props = $props();

  const all = $derived(dedupe((ids ?? []).filter((x): x is string => !!x).map((x) => stripStep(String(x)))));
  let showAll = $state(false);
  const open = $derived(moreKey ? showAll : true);
  const shown = $derived(limit && !open ? all.slice(0, limit) : all);
  let expanded = $state<Record<string, boolean>>({});

  function statement(id: string) {
    return statementOf(book.get(id)?.fields ?? {}) ?? "";
  }
</script>

{#if all.length}
  <ol class="m-0 list-decimal border-t border-slate-200 pr-0 pb-0 pl-8">
    {#each shown as id (id)}
      {@const t = book.typeOf(id)}
      {@const stmt = statement(id)}
      {@const isExpanded = expanded[id] ?? false}
      <li class="border-b border-slate-200 py-2 text-sm marker:font-mono marker:text-xs marker:text-slate-600">
        {#if book.has(id)}
          <a
            href={"#" + encodeURIComponent(id)}
            data-go={id}
            class="font-medium text-blue-700 underline decoration-slate-300 underline-offset-2 hover:decoration-blue-700"
          >
            {book.title(id)}
          </a>
        {:else}
          <span class="text-slate-600" title="No record with this identifier is in the book">{id}</span>
        {/if}
        <span class="float-right inline-flex items-center gap-2 text-xs text-slate-600">
          <TypeTag type={t} />
        </span>
        {#if stmt}
          <div class="mt-1 text-xs leading-snug text-slate-600">
            {#if stmt.length <= 220}
              {stmt}
            {:else}
              {isExpanded ? stmt : stmt.slice(0, 220) + "..."}{" "}
              <button
                type="button"
                class="cursor-pointer font-semibold text-blue-700 underline underline-offset-2"
                onclick={() => (expanded[id] = !isExpanded)}
              >
                {isExpanded ? "Show less" : "Show more"}
              </button>
            {/if}
          </div>
        {/if}
      </li>
    {/each}
  </ol>
  {#if limit && !open && all.length > shown.length}
    <Button variant="outline" size="sm" class="mt-2" onclick={() => (showAll = true)}>Show all {all.length}</Button>
  {/if}
{/if}
