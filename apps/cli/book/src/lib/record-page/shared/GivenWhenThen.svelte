<script lang="ts">
  // The Given, When and Then of a scenario, as one grid.
  type Props = { given: string; when: string; then: string };
  let { given, when, then }: Props = $props();
</script>

<dl class="grid grid-cols-[56px_minmax(0,1fr)] gap-x-3 gap-y-1.5 text-sm">
  <dt class="pt-0.5 font-mono text-[0.71875rem] font-medium tracking-[0.06em] text-muted-foreground uppercase">Given</dt>
  <dd class="m-0">{given}</dd>
  <dt class="pt-0.5 font-mono text-[0.71875rem] font-medium tracking-[0.06em] text-muted-foreground uppercase">When</dt>
  <dd class="m-0">{when}</dd>
  <dt class="pt-0.5 font-mono text-[0.71875rem] font-medium tracking-[0.06em] text-muted-foreground uppercase">Then</dt>
  <dd class="m-0">{then}</dd>
</dl>
