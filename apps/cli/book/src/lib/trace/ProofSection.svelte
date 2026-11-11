<script lang="ts">
  import { book, pluralize, stripStep } from "$lib/frame";
  import type { BookTest } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import { Separator } from "$lib/ui/separator";
  import EditorFileLink from "./EditorFileLink.svelte";

  // The proof section: the tests that prove the record's scenarios, or the
  // tests that prove the record itself.
  let { id }: { id: string | null } = $props();
  const rec = $derived(id ? book.get(id) : undefined);
  const show = $derived(!!rec && !!rec.expect && !!rec.expect.proof);
  const isFlow = $derived(rec?.type === "use-case" || rec?.type === "reaction" || rec?.type === "job");
  const hasRun = $derived(!!book.man?.testRun?.date);
  const typeWord = $derived(rec ? book.typeName(rec.type).toLowerCase() : "");
  const sc = $derived(id && isFlow ? book.scenariosOf(id).filter((x) => book.get(stripStep(x))) : []);
  const none = $derived(sc.filter((x) => !(book.get(stripStep(x))?.tests ?? []).length));
  const proved = $derived(sc.filter((x) => (book.get(stripStep(x))?.tests ?? []).length).slice(0, 6));
  const tests = $derived(rec?.tests ?? []);
  const count = $derived(isFlow ? sc.length - none.length + " of " + sc.length : String(tests.length));
  const summary = $derived(
    pluralize(sc.length - none.length, "scenario") +
      " proved by a test" +
      (none.length ? ", " + pluralize(none.length, "scenario") + " with no test" : "") +
      "." +
      (hasRun ? "" : " No test run is in the model."),
  );

  function firstTest(x: string): BookTest | null {
    const list = book.get(stripStep(x))?.tests ?? [];
    return list.length ? list[0] : null;
  }
</script>

{#if show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="Proof" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <RecordIcon name="flask" class="size-3.5" />
      <button type="button" data-explain="awaiting" class="uppercase">Proof</button>
      <span class="ml-auto font-mono text-[11px] font-normal tracking-normal text-muted-foreground">{count}</span>
    </h3>
    {#if isFlow}
      {#if !sc.length}
        <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
          <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0" />
          <span>This use case has no scenarios, so nothing can prove it.</span>
        </p>
      {:else}
        <p class="mb-1.5 text-[12.5px] text-muted-foreground">{summary}</p>
        {#each none as x}
          <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
            <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0" />
            <span>No test proves <RecordLink id={x} /></span>
          </p>
        {/each}
        {#each proved as x}
          {@const t = firstTest(x)}
          {#if t}
            <div class="border-b border-border py-1.5 last:border-b-0">
              <span class="mb-px block text-[11.5px] text-muted-foreground">{book.title(x)}</span>
              <EditorFileLink path={String(t.path ?? t.file ?? "")} line={typeof t.line === "number" ? t.line : null} label={String(t.name ?? "")} />
            </div>
          {/if}
        {/each}
      {/if}
    {:else if !tests.length}
      <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
        <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0" />
        <span>No test proves this {typeWord}.</span>
      </p>
    {:else}
      {#each tests as t}
        <div class="border-b border-border py-1.5 last:border-b-0">
          <span class="mb-px block text-[11.5px] text-muted-foreground">{t.browser ? "Browser test" : "Test"}{t.confirmed ? "" : ", not confirmed"}</span>
          <EditorFileLink path={String(t.path ?? t.file ?? "")} line={typeof t.line === "number" ? t.line : null} label={String(t.name ?? "")} />
        </div>
      {/each}
      {#if !hasRun}
        <p class="mb-1.5 text-[12.5px] text-muted-foreground">No test run is in the model.</p>
      {/if}
    {/if}
  </section>
{/if}
