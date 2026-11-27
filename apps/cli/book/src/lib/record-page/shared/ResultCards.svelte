<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeOf, muted, outcomeTag, refLink, td } from "./record-cells";

  // Results as one table: the outcome tag, the result, its meaning and its code.
  type Props = {
    items: { id?: string | null; title: string; outcome: string; meaning: string; code?: string | null; kind?: string }[];
    caption?: string | null;
  };
  let { items, caption = null }: Props = $props();

  // The refusals the model derives instead of a written result.
  const GENERIC_SUFFIXES = [".forbidden", ".invalid-input", ".not-found", ".conflict", ".unauthenticated", ".sign-in-too-old", ".rule-not-stated"];

  function isGeneric(x: { id?: string | null; kind?: string }): boolean {
    if (x.kind === "generic") return true;
    const id = x.id ?? "";
    return GENERIC_SUFFIXES.some((s) => id.endsWith(s));
  }

  function messageOf(id: string | null | undefined): string {
    if (!id) return "";
    const s = book.get(stripStep(id))?.fields?.message;
    return typeof s === "string" ? s : "";
  }

  function resultCell(x: { id?: string | null; title: string; kind?: string }): string {
    if (isGeneric(x)) return `<span data-explain="g:generic refusal" tabindex="0" class="cursor-help">${esc(x.title)}</span>`;
    if (x.id && book.has(stripStep(x.id))) return refLink(book, x.id, x.title);
    return esc(x.title);
  }
</script>

{#if items?.length}
  <DataTable
    head={["Outcome", "Result", "Meaning", "Code"]}
    {caption}
    rows={items.map((x) => {
      const message = messageOf(x.id ?? null);
      return [
        td(outcomeTag(x.outcome)),
        td(resultCell(x)),
        td(esc(x.meaning) + (message ? muted(`&ldquo;${esc(message)}&rdquo;`) : "")),
        td(x.code ? codeOf(x.code) : ""),
      ];
    })}
  />
{/if}
