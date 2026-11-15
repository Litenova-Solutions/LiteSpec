<script lang="ts">
  import { book, sourceLinkHref } from "$lib/frame";

  // A file in the project checkout: its name with the directory and line below,
  // opening as a source link when the manifest names one.
  type Props = { path: string; line?: number | null; label?: string };
  let { path, line = null, label = "" }: Props = $props();
  const href = $derived(sourceLinkHref(book, path, line));
  const name = $derived(label || path.split("/").pop() || path);
  const dir = $derived(path.split("/").slice(0, -1).join("/"));
  const title = $derived("Open " + path + (line ? " at line " + line : ""));
</script>

{#if href}
  <a class="block text-slate-900" {href} {title}>
    <span class="block text-sm font-medium break-all hover:underline">{name}</span>
    <span class="block font-mono text-xs break-all text-slate-600">{dir}{#if line}:{line}{/if}</span>
  </a>
{:else}
  <span class="block">
    <span class="block text-sm font-medium break-all">{name}</span>
    <span class="block font-mono text-xs break-all text-slate-600">{dir}{#if line}:{line}{/if}</span>
  </span>
{/if}
