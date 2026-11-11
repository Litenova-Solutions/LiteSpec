<script lang="ts">
  // A fact: book.js rendered every badge as plain emphasized text.
  let { text }: { text: string } = $props();
</script>

<span class="font-medium text-foreground">{text}</span>
