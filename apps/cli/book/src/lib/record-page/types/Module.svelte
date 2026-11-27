<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { TypeTag } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import { stringList } from "../page-values";

  // A module: what it covers, what it excludes, and how many records of each behavior kind it holds.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const covers = $derived(stringList(r.fields ?? {}, "covers"));
  const excludes = $derived(stringList(r.fields ?? {}, "excludes"));
  const KINDS = ["aggregate", "use-case", "rule", "domain-event", "reaction", "permission", "job", "notification", "setting", "read-model", "reference-data"];
  const all = $derived(KINDS.flatMap((t) => book.descByType(id, t, 6)));
  const rows = $derived(
    KINDS.map((t) => ({ t, n: all.filter((x) => book.typeOf(x) === t).length }))
      .filter((x) => x.n > 0)
      .sort((a, b) => b.n - a.n),
  );
</script>

{#if covers.length}
  <PageSection blockKey="resp" heading="covers" count={covers.length}>
    <BulletList items={covers} />
  </PageSection>
{/if}
{#if excludes.length}
  <PageSection blockKey="exc" heading="excludes" count={excludes.length}>
    <BulletList items={excludes} />
  </PageSection>
{/if}
{#if all.length}
  <PageSection blockKey="size" heading="Records">
    <!-- A scrollable region stays keyboard reachable through its tabindex. -->
    <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
    <div class="overflow-x-auto" tabindex="0" role="region" aria-labelledby="mod-records-cap">
      <table class="w-full text-sm">
        <caption id="mod-records-cap" class="sr-only">Record counts of {book.title(id)}</caption>
        <thead>
          <tr>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold"><span class="sr-only">Badge</span></th>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Kind</th>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Count</th>
          </tr>
        </thead>
        <tbody>
          {#each rows as row (row.t)}
            <tr class="hover:[&_td]:bg-layer-hover hover:[&_th]:bg-layer-hover">
              <td class="border-t border-border-subtle bg-layer px-4 py-3 align-top"><TypeTag type={row.t} /></td>
              <th scope="row" class="border-t border-border-subtle bg-layer px-4 py-3 text-left align-top font-normal">{book.pluralName(row.t)}</th>
              <td class="border-t border-border-subtle bg-layer px-4 py-3 text-right align-top tabular-nums">{row.n}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </PageSection>
{/if}
