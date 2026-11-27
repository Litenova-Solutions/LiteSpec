<script lang="ts">
  // The Given, When and Then of a scenario, as one grid.
  type Props = { given: string; when: string; then: string };
  let { given, when, then }: Props = $props();
</script>

<dl class="grid grid-cols-[4.5rem_minmax(0,1fr)] gap-x-4 gap-y-2 text-base">
  <dt class="font-mono text-xs leading-6 tracking-[0.32px] text-text-secondary uppercase">Given</dt>
  <dd class="m-0">{given}</dd>
  <dt class="font-mono text-xs leading-6 tracking-[0.32px] text-text-secondary uppercase">When</dt>
  <dd class="m-0">{when}</dd>
  <dt class="font-mono text-xs leading-6 tracking-[0.32px] text-text-secondary uppercase">Then</dt>
  <dd class="m-0">{then}</dd>
</dl>
