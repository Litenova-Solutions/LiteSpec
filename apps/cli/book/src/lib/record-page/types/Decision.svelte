<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { proseOf } from "../page-prose";

  // A decision: the choice, the options weighed, and when it reopens.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const choice = $derived(f.choice ? String(f.choice) : "");
  const opts = $derived(((f.options as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const consequences = $derived(proseOf(r, "consequences"));
</script>

{#if choice}
  <Note kind="info" icon="passing">
    <b>Choice:</b>{" "}
    {choice}
  </Note>
{/if}
{#if consequences}
  <PageSection blockKey="cons" heading="consequences"><RenderedProse html={consequences} /></PageSection>
{/if}
{#if opts.length}
  <PageSection blockKey="opts" heading="options" count={opts.length}>
    <div class="border-t border-border">
      {#each opts as o}
        {@const chosen = !!choice && String(o.name) === choice}
        <div class="border-b border-border py-2.5 {chosen ? 'font-medium' : ''}">
          <h3 class="m-0 mb-1.5 text-[14.5px] leading-snug font-semibold">
            {String(o.name ?? "")}
            {#if chosen}{" "}<Fact text="Choice" />{/if}
          </h3>
          <div class="grid max-w-[72ch] grid-cols-[18px_1fr] gap-x-1.5 gap-y-1 text-[13.5px] leading-snug text-subtle-foreground">
            <span class="text-convention-foreground"><RecordIcon name="plus" class="size-3.5" /></span>
            <span>{String(o.pros ?? "")}</span>
            <span class="text-warn-foreground"><RecordIcon name="minus" class="size-3.5" /></span>
            <span>{String(o.cons ?? "")}</span>
          </div>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if f.reopenedWhen}
  <PageSection blockKey="rev" heading="reopenedWhen">
    <Note kind="quiet" icon="clock">{String(f.reopenedWhen)}</Note>
  </PageSection>
{/if}
