<script lang="ts">
  import { QueryClientProvider } from "@tanstack/svelte-query";
  import type { LayoutProps } from "./$types";

  let { data, children }: LayoutProps = $props();
</script>

<QueryClientProvider client={data.queryClient}>
  {@render children()}
</QueryClientProvider>
