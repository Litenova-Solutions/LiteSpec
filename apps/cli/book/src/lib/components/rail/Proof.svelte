<script lang="ts">
  import { base, book, plural } from "$lib/book";
  import type { BookTest } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import { Separator } from "$lib/components/ui/separator";
  import FileLink from "./FileLink.svelte";

  // The proof section: the tests that prove the record's scenarios, or the
  // tests that prove the record itself.
  let { id }: { id: string | null } = $props();
  const rec = $derived(id ? book.get(id) : undefined);
  const show = $derived(!!rec && !!rec.expect && !!rec.expect.proof);
  const isFlow = $derived(rec?.type === "use-case" || rec?.type === "reaction" || rec?.type === "job");
  const hasRun = $derived(!!book.man?.testRun?.date);
  const typeWord = $derived(rec ? book.typeName(rec.type).toLowerCase() : "");
  const sc = $derived(id && isFlow ? book.scenariosOf(id).filter((x) => book.get(base(x))) : []);
  const none = $derived(sc.filter((x) => !(book.get(base(x))?.tests ?? []).length));
  const proved = $derived(sc.filter((x) => (book.get(base(x))?.tests ?? []).length).slice(0, 6));
  const tests = $derived(rec?.tests ?? []);
  const count = $derived(isFlow ? sc.length - none.length + " of " + sc.length : String(tests.length));
  const summary = $derived(
    plural(sc.length - none.length, "scenario") +
      " proved by a test" +
      (none.length ? ", " + plural(none.length, "scenario") + " with no test" : "") +
      "." +
      (hasRun ? "" : " No test run is in the model."),
  );

  function firstTest(x: string): BookTest | null {
    const list = book.get(base(x))?.tests ?? [];
    return list.length ? list[0] : null;
  }
</script>

{#if show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="Proof" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <Icon name="flask" class="size-3.5" />
      <button type="button" data-explain="awaiting" class="uppercase">Proof</button>
      <span class="ml-auto font-mono text-[11px] font-normal tracking-normal text-muted-foreground">{count}</span>
    </h3>
    {#if isFlow}
      {#if !sc.length}
        <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
          <Icon name="missing" class="mt-0.5 size-3.5 shrink-0" />
          <span>This use case has no scenarios, so nothing can prove it.</span>
        </p>
      {:else}
        <p class="mb-1.5 text-[12.5px] text-muted-foreground">{summary}</p>
        {#each none as x}
          <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
            <Icon name="missing" class="mt-0.5 size-3.5 shrink-0" />
            <span>No test proves <InlineRef id={x} /></span>
          </p>
        {/each}
        {#each proved as x}
          {@const t = firstTest(x)}
          {#if t}
            <div class="border-b border-border py-1.5 last:border-b-0">
              <span class="mb-px block text-[11.5px] text-muted-foreground">{book.title(x)}</span>
              <FileLink path={String(t.path ?? t.file ?? "")} line={typeof t.line === "number" ? t.line : null} label={String(t.name ?? "")} />
            </div>
          {/if}
        {/each}
      {/if}
    {:else if !tests.length}
      <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
        <Icon name="missing" class="mt-0.5 size-3.5 shrink-0" />
        <span>No test proves this {typeWord}.</span>
      </p>
    {:else}
      {#each tests as t}
        <div class="border-b border-border py-1.5 last:border-b-0">
          <span class="mb-px block text-[11.5px] text-muted-foreground">{t.browser ? "Browser test" : "Test"}{t.confirmed ? "" : ", not confirmed"}</span>
          <FileLink path={String(t.path ?? t.file ?? "")} line={typeof t.line === "number" ? t.line : null} label={String(t.name ?? "")} />
        </div>
      {/each}
      {#if !hasRun}
        <p class="mb-1.5 text-[12.5px] text-muted-foreground">No test run is in the model.</p>
      {/if}
    {/if}
  </section>
{/if}
