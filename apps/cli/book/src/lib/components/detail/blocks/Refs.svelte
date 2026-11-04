<script lang="ts">
  import { base, book, uniq } from "$lib/book";
  import StatusIcon from "$lib/components/book/StatusIcon.svelte";
  import TypeTag from "$lib/components/book/TypeTag.svelte";
  import { Button } from "$lib/components/ui/button";

  // Records as a numbered list: the title link, the type tag, the status, and
  // the expandable statement. Long lists keep a limit with a Show all button.
  let {
    ids,
    limit = null,
    moreKey = "",
  }: {
    ids: (string | null | undefined)[];
    limit?: number | null;
    moreKey?: string;
  } = $props();

  const all = $derived(uniq((ids ?? []).filter((x): x is string => !!x).map((x) => base(String(x)))));
  const open = $derived(moreKey ? !!book.more[moreKey] : true);
  const shown = $derived(limit && !open ? all.slice(0, limit) : all);

  function statement(id: string) {
    const f = book.get(id)?.fields ?? {};
    return String(f.statement ?? f.meaning ?? f.summary ?? f.description ?? f.offering ?? "");
  }
</script>

{#if all.length}
  <ol class="m-0 list-decimal border-t border-border pr-0 pb-0 pl-7">
    {#each shown as id (id)}
      {@const t = book.typeOf(id)}
      {@const stmt = statement(id)}
      {@const expanded = !!book.more["stmt|" + id]}
      <li class="border-b border-border py-2 text-[14.5px] marker:font-mono marker:text-xs marker:text-muted-foreground">
        {#if book.has(id)}
          <button type="button" class="font-medium underline decoration-strong underline-offset-2 hover:decoration-foreground" data-go={id}>
            {book.title(id)}
          </button>
        {:else}
          <span class="text-muted-foreground" title="No record with this identifier is in the book">{id}</span>
        {/if}
        <span class="float-right inline-flex items-center gap-2 text-xs text-muted-foreground">
          <TypeTag type={t} />
          {#if book.get(id)}<StatusIcon id={id} />{/if}
        </span>
        {#if stmt}
          <div class="mt-0.5 text-[13px] leading-snug text-muted-foreground">
            {#if stmt.length <= 220}
              {stmt}
            {:else}
              {expanded ? stmt : stmt.slice(0, 220) + "..."}{" "}
              <button
                type="button"
                class="cursor-pointer font-semibold text-highlight-foreground underline underline-offset-2"
                onclick={() => (book.more["stmt|" + id] = !expanded)}
              >
                {expanded ? "Show less" : "Show more"}
              </button>
            {/if}
          </div>
        {/if}
      </li>
    {/each}
  </ol>
  {#if limit && !open && all.length > shown.length}
    <Button variant="outline" size="sm" class="mt-2" onclick={() => (book.more[moreKey] = true)}>Show all {all.length}</Button>
  {/if}
{/if}
