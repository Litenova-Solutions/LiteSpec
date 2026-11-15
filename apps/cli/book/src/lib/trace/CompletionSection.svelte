<script lang="ts">
  import { book, completionLines } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The Completion section: whether the record has the code and tests it
  // expects, and how many of its scenarios pass. Each line reads only when
  // the record expects it.
  type Props = { id: string | null };
  let { id }: Props = $props();
  const r = $derived(id ? book.get(id) : undefined);
  const lines = $derived(r ? completionLines(r.completion).filter((l) => l.mark !== "none-expected") : []);
  const scenariosOf = $derived(r?.completion?.scenarios?.of ?? 0);
</script>

{#if lines.length}
  <section aria-labelledby="rail-completion-h" class="mb-8 last:mb-0">
    <h2 id="rail-completion-h" class="mb-4 text-sm font-semibold">Completion</h2>
    <ul class="m-0 grid list-none gap-3 p-0 text-sm">
      {#each lines as line (line.label)}
        {@const present = line.mark === "present"}
        {@const neutral = !present && line.label.includes("scenario") && scenariosOf > 0}
        <li class="flex items-center gap-2">
          <RecordIcon
            name={present ? "done" : neutral ? "minus" : "alert"}
            class="size-4 shrink-0 {present ? 'text-support-success' : neutral ? '' : 'text-warning-ink'}"
          />
          <span class={present || neutral ? "" : "text-warning-ink"}>{line.label}{#if line.detail} {line.detail}{/if}</span>
        </li>
      {/each}
    </ul>
  </section>
{/if}
