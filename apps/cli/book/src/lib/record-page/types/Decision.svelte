<script lang="ts">
  import { book, dedupe, hoverState, inline } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { Hover, RenderedProse, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import Note from "../blocks/Note.svelte";
  import { proseOf } from "../page-prose";
  import { objectList, stringField } from "../page-values";

  // A decision: the choice, what it supersedes and what supersedes it, the options
  // weighed, the code following it by module, and when it reopens.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const choice = $derived(stringField(f, "choice") ?? "");
  const opts = $derived(objectList(f, "options"));
  const consequences = $derived(proseOf(r, "consequences"));
  const reopenedWhen = $derived(stringField(f, "reopenedWhen"));
  const supersedes = $derived(dedupe(book.outs(id, "supersedes")).sort());
  const supersededBy = $derived(dedupe(book.ins(id, "supersedes")).sort());
  const followers = $derived(
    dedupe(book.ins(id, "follows").map((target) => (target.startsWith("code/") ? target.slice("code/".length) : target))).sort(),
  );
  const followerGroups = $derived.by(() => {
    const modules = new Map<string, string>();
    for (const candidate of book.order) {
      if (book.typeOf(candidate) !== "module") continue;
      const name = candidate.split("/")[1] ?? "";
      if (name && !modules.has(name.toLowerCase())) modules.set(name.toLowerCase(), candidate);
    }
    const groups = new Map<string, { label: string; paths: string[] }>();
    for (const path of followers) {
      let found = "";
      for (const segment of path.split("/")) {
        const hit = modules.get(segment.toLowerCase());
        if (hit) {
          found = hit;
          break;
        }
      }
      let group = groups.get(found);
      if (!group) {
        group = { label: found ? book.title(found) : "No module", paths: [] };
        groups.set(found, group);
      }
      group.paths.push(path);
    }
    return [...groups.entries()]
      .map(([key, group]) => ({ key, label: group.label, paths: group.paths.sort() }))
      .sort((a, b) => {
        if (a.key === "" && b.key !== "") return 1;
        if (b.key === "" && a.key !== "") return -1;
        if (a.label === b.label) return 0;
        return a.label < b.label ? -1 : 1;
      });
  });
</script>

{#if choice}
  <Note kind="good" icon="passing">
    <b>{book.fieldLabel("choice")}</b><br />{choice}
  </Note>
{/if}
{#if consequences}
  <PageSection blockKey="cons" heading="consequences"><RenderedProse html={consequences} /></PageSection>
{/if}
{#if opts.length}
  <PageSection blockKey="opts" heading="options" count={opts.length}>
    <div class="grid grid-cols-[repeat(auto-fit,minmax(12rem,1fr))] gap-px border border-border-subtle bg-border-subtle">
      {#each opts as o, i (o.name ?? i)}
        {@const chosen = !!choice && String(o.name) === choice}
        <div class="bg-background p-4 text-sm">
          <p class="m-0">
            <b>{String(o.name ?? "")}</b>
            {#if chosen}{" "}<Hover hover={hoverState(book, "g:chosen")}><span data-explain="g:chosen" class="inline-flex h-[18px] items-center gap-1 rounded-full bg-tag-green px-2 text-xs tracking-[0.32px] text-tag-green-foreground">Chosen</span></Hover>{/if}
          </p>
          <div class="mt-3 text-xs tracking-[0.32px] text-text-secondary">{book.fieldLabel("pros")}</div>
          <p class="mt-1 mb-0">{String(o.pros ?? "")}</p>
          <div class="mt-3 text-xs tracking-[0.32px] text-text-secondary">{book.fieldLabel("cons")}</div>
          <p class="mt-1 mb-0">{String(o.cons ?? "")}</p>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if supersedes.length}
  <PageSection blockKey="sup" heading="supersedes" count={supersedes.length}>
    <RecordList ids={supersedes} />
  </PageSection>
{/if}
{#if supersededBy.length}
  <PageSection blockKey="supby" heading="Superseded by" count={supersededBy.length}>
    <RecordList ids={supersededBy} />
  </PageSection>
{/if}
{#if followers.length}
  <PageSection blockKey="fol" heading="Followers" count={followers.length}>
    <div class="border-t border-border-subtle">
      {#each followerGroups as g (g.key || "none")}
        <div class="border-b border-border-subtle py-2.5">
          <h3 class="m-0 mb-1.5 text-sm leading-snug font-semibold">
            {g.label}
          </h3>
          <ul class="m-0 list-none p-0">
            {#each g.paths as p (p)}
              <li class="py-0.5"><SourceLink src={{ path: p }} /></li>
            {/each}
          </ul>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if reopenedWhen}
  <PageSection blockKey="rev" heading="reopenedWhen">
    <RenderedProse html={inline(book, reopenedWhen, r.src ?? null)} />
  </PageSection>
{/if}
