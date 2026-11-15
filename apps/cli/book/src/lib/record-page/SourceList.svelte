<script lang="ts">
  import { book, safeHref } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import { codeCls } from "./page-html";
  import { stringField } from "./page-values";

  // The sources a research or constraint record rests on: each key, its link or path, and when it was read.
  type Props = { sources: RecordFields[] };
  let { sources }: Props = $props();

  function hostOf(url: string) {
    try {
      return new URL(url).host.replace(/^www\./, "");
    } catch {
      return "";
    }
  }
</script>

{#if sources.length}
  <PageSection blockKey="srcs" heading="Sources" count={sources.length}>
    <ol class="m-0 list-decimal border-t border-slate-200 p-0 pl-8">
      {#each sources as x, i (String(x.key ?? i))}
        {@const readOn = stringField(x, "readOn")}
        {@const published = stringField(x, "publishedOn")}
        {@const dates = [published ? "published " + book.fmtDate(published) : "", readOn ? "read " + book.fmtDate(readOn) : ""].filter(Boolean).join(", ")}
        <li class="border-b border-slate-200 py-2 text-sm marker:font-mono marker:text-xs marker:text-slate-600">
          {#if x.path}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            <span class={codeCls}>{String(x.path)}</span>
            {#if x.title} {String(x.title)}{/if}
            <span class="mt-1 block text-xs text-slate-600">{dates}</span>
          {:else}
            {@const href = safeHref(book, x.url)}
            {@const label = String(x.title ?? String(x.url ?? "").replace(/^https?:\/\/(www\.)?/, ""))}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            {#if href}
              <a href={href} target="_blank" rel="noopener" class="text-blue-700 underline decoration-slate-300 underline-offset-2 hover:decoration-blue-700">{label}</a>
            {:else}
              {label}
            {/if}
            <span class="mt-1 block text-xs text-slate-600">
              {hostOf(String(x.url ?? ""))}{dates ? ", " + dates : ""}
            </span>
          {/if}
        </li>
      {/each}
    </ol>
  </PageSection>
{/if}
