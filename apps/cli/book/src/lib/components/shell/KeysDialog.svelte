<script lang="ts">
  import * as Dialog from "$lib/components/ui/dialog";
  import { Kbd } from "$lib/components/ui/kbd";

  // The keyboard shortcuts dialog, with the exact list from the old book.
  let { open = $bindable(false) }: { open?: boolean } = $props();
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="sm:max-w-lg">
    <Dialog.Header>
      <Dialog.Title>Keyboard shortcuts</Dialog.Title>
    </Dialog.Header>
    <dl class="grid grid-cols-[150px_1fr] items-center gap-x-4 gap-y-2.5 text-sm">
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>/</Kbd> or <Kbd>Ctrl K</Kbd></dt>
      <dd>Filter the navigation</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>j</Kbd><Kbd>k</Kbd></dt>
      <dd>Next and previous record in the navigation</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>u</Kbd></dt>
      <dd>Up to the parent record</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>[</Kbd><Kbd>]</Kbd></dt>
      <dd>Previous and next sibling</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>1</Kbd> to <Kbd>2</Kbd></dt>
      <dd>Outline, Security</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>e</Kbd><Kbd>c</Kbd></dt>
      <dd>Expand or collapse the outline</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>Alt</Kbd><Kbd>&larr;</Kbd></dt>
      <dd>Back to the previous record</dd>
      <dt class="flex flex-wrap items-center justify-end gap-1"><Kbd>Esc</Kbd></dt>
      <dd>Clear the filter, close this</dd>
    </dl>
  </Dialog.Content>
</Dialog.Root>
