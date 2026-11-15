<script lang="ts">
  // A fact: book.js rendered every badge as plain emphasized text.
  type Props = { text: string };
  let { text }: Props = $props();
</script>

<span class="font-medium text-foreground">{text}</span>
