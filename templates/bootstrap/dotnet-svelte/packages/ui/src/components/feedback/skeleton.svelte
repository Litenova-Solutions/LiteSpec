<script lang="ts">
  import { cn } from "../../utils/cn.ts";

  type Props = {
    class?: string;
  };

  let { class: className = "" }: Props = $props();
</script>

<div class={cn("animate-pulse rounded-md bg-muted", className)} aria-hidden="true"></div>
