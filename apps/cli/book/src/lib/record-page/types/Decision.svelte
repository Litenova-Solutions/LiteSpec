<script lang="ts">
  import { book, dedupe } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, RenderedProse, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { proseOf } from "../page-prose";
  import { objectList, stringField } from "../page-values";

  // A decision: the choice, the options weighed, the code following it by module, and when it reopens.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const choice = $derived(stringField(f, "choice") ?? "");
  const opts = $derived(objectList(f, "options"));
  const consequences = $derived(proseOf(r, "consequences"));
  const reopenedWhen = $derived(stringField(f, "reopenedWhen"));
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
      {#each opts as o, i (o.name ?? i)}
        {@const chosen = !!choice && String(o.name) === choice}
        <div class="border-b border-border py-2.5 {chosen ? 'font-medium' : ''}">
          <h3 class="m-0 mb-1.5 text-[0.90625rem] leading-snug font-semibold">
            {String(o.name ?? "")}
            {#if chosen}{" "}<Fact text="Choice" />{/if}
          </h3>
          <div class="grid max-w-[72ch] grid-cols-[18px_1fr] gap-x-1.5 gap-y-1 text-[0.84375rem] leading-snug text-subtle-foreground">
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
{#if followers.length}
  <PageSection blockKey="fol" heading="Followers" count={followers.length}>
    <div class="border-t border-border">
      {#each followerGroups as g (g.key || "none")}
        <div class="border-b border-border py-2.5">
          <h3 class="m-0 mb-1.5 text-[0.90625rem] leading-snug font-semibold">
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
    <Note kind="quiet" icon="clock">{reopenedWhen}</Note>
  </PageSection>
{/if}
