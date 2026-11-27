<script lang="ts">
  import { book, dedupe, stripStep } from "$lib/frame";
  import { TypeTag } from "$lib/record";
  import { statementOf } from "../page-prose";

  // Records as a contained list: the kind badge, the title link and the
  // statement excerpt. Long lists keep a limit with a Show all button.
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
  <ul class="border-t border-border-subtle">
    {#each shown as id (id)}
      {@const t = book.typeOf(id)}
      {@const stmt = statement(id)}
      {@const isExpanded = expanded[id] ?? false}
      {@const known = book.has(id)}
      <li class="border-b border-border-subtle">
        {#if known}
          <a
            href={"#" + encodeURIComponent(id)}
            data-go={id}
            class="grid grid-cols-[20px_minmax(0,1fr)] gap-x-3 gap-y-0.5 py-3 pr-4 hover:bg-layer-hover"
          >
            <TypeTag type={t} />
            <span class="text-link">{book.title(id)}</span>
            {#if stmt}
              <span class="col-start-2 text-sm text-text-secondary">
                {stmt.length <= 220 || isExpanded ? stmt : stmt.slice(0, 220) + "..."}
              </span>
            {/if}
          </a>
        {:else}
          <div class="grid grid-cols-[20px_minmax(0,1fr)] gap-x-3 gap-y-0.5 py-3 pr-4">
            <TypeTag type={t} />
            <span class="font-mono text-xs text-text-secondary">{id}</span>
          </div>
        {/if}
        {#if stmt.length > 220}
          <div class="pr-4 pb-3 pl-8">
            <button
              type="button"
              class="cursor-pointer text-sm text-link hover:underline"
              onclick={() => (expanded[id] = !isExpanded)}
            >
              {isExpanded ? "Show less" : "Show more"}
            </button>
          </div>
        {/if}
      </li>
    {/each}
  </ul>
  {#if limit && !open && all.length > shown.length}
    <button type="button" class="mt-2 inline-flex min-h-8 cursor-pointer items-center px-4 text-sm text-link hover:bg-hover-wash" onclick={() => (showAll = true)}>Show all {all.length}</button>
  {/if}
{/if}
