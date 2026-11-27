<script lang="ts">
  import { pageSections } from "$lib/record";

  // The "On this page" list: the shown sections of the open record, in page
  // order. The links scroll within the record; they never write the hash, so
  // the open record stays put.
  const items = $derived(pageSections.items);

  function jump(key: string): void {
    const section = document.getElementById("b-" + key);
    section?.scrollIntoView();
    section?.querySelector("h2")?.focus({ preventScroll: true });
  }
</script>

{#if items.length >= 2}
  <nav aria-labelledby="rail-contents-h" class="mb-8 last:mb-0">
    <h2 id="rail-contents-h" class="mb-4 text-sm font-semibold">On this page</h2>
    <div>
      {#each items as x, i (x.key)}
        <a
          href={"#b-" + x.key}
          aria-current={i === 0 ? "true" : undefined}
          onclick={(e) => {
            e.preventDefault();
            jump(x.key);
          }}
          class="flex justify-between gap-3 border-l-2 py-1.5 pr-2 pl-4 text-sm hover:bg-hover-wash {i === 0
            ? 'border-interactive font-semibold text-foreground'
            : 'border-border-subtle text-text-secondary hover:text-foreground'}"
        >
          <span class="min-w-0 break-words">{x.label}</span>
          {#if x.count !== null}<span class="shrink-0 text-xs text-text-helper tabular-nums">{x.count}</span>{/if}
        </a>
      {/each}
    </div>
  </nav>
{/if}
