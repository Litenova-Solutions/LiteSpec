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
    <ol class="m-0 list-decimal border-t border-border p-0 pl-7">
      {#each sources as x, i (String(x.key ?? i))}
        {@const readOn = stringField(x, "readOn")}
        <li class="border-b border-border py-2 text-sm marker:font-mono marker:text-xs marker:text-muted-foreground">
          {#if x.path}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            <span class={codeCls}>{String(x.path)}</span>
            {#if x.title} {String(x.title)}{/if}
            <span class="mt-px block text-xs text-muted-foreground">{readOn ? "read " + book.fmtDate(readOn) : ""}</span>
          {:else}
            {@const href = safeHref(book, x.url)}
            {@const label = String(x.title ?? String(x.url ?? "").replace(/^https?:\/\/(www\.)?/, ""))}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            {#if href}
              <a href={href} target="_blank" rel="noopener">{label}</a>
            {:else}
              {label}
            {/if}
            <span class="mt-px block text-xs text-muted-foreground">
              {hostOf(String(x.url ?? ""))}{readOn ? ", read " + book.fmtDate(readOn) : ""}
            </span>
          {/if}
        </li>
      {/each}
    </ol>
  </PageSection>
{/if}
