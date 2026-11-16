<script lang="ts">
  import { book, completionLines, stageLine } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";

  // The Completion section: the model stage with its scope counts and parked
  // children, and whether the record has the code and tests it expects, with
  // how many of its scenarios pass. Each line reads only when the record
  // expects it.
  type Props = { id: string | null };
  let { id }: Props = $props();
  const r = $derived(id ? book.get(id) : undefined);
  const stage = $derived(r ? stageLine(r.completion) : null);
  const parkedBy = $derived(r?.completion?.parkedBy ?? []);
  const lines = $derived(r ? completionLines(r.completion).filter((l) => l.mark !== "none-expected") : []);
  const scenariosOf = $derived(r?.completion?.scenarios?.of ?? 0);

  function expiryText(expiresOn: string | null | undefined): string {
    if (!expiresOn) return "Lasting, no expiry date";
    return "expires " + book.fmtDate(expiresOn);
  }
</script>

{#if stage || parkedBy.length || lines.length}
  <section aria-labelledby="rail-completion-h" class="mb-8 last:mb-0">
    <h2 id="rail-completion-h" class="mb-4 text-sm font-semibold">Completion</h2>
    {#if stage}
      <p class="m-0 mb-3 text-sm">{stage}</p>
    {/if}
    {#if parkedBy.length}
      <ul class="m-0 mb-3 grid list-none gap-2 p-0 text-sm">
        {#each parkedBy as p (p.id)}
          <li class="flex flex-wrap items-center gap-x-1.5 gap-y-0.5">
            <RecordLink id={p.id} />
            <span class="text-text-secondary">parked by</span>
            <RecordLink id={p.waiver} />
            <span class="text-text-secondary">{expiryText(p.expiresOn)}</span>
          </li>
        {/each}
      </ul>
    {/if}
    {#if lines.length}
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
    {/if}
  </section>
{/if}
