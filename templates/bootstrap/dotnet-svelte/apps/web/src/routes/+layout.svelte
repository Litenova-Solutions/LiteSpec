<script lang="ts">
  import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
  import { TextLink } from "@__SCOPE__/ui/floorplans";
  import "../styles.css";
  import type { LayoutProps } from "./$types";

  let { children }: LayoutProps = $props();

  // One query client for the shell: every route reads with svelte-query
  // against it, and each write invalidates the keys it changed. A read that
  // fails is not retried, as in the loaders, so the page shows its problem
  // state at once and the reader tries again by reloading.
  const queryClient = new QueryClient({
    defaultOptions: { queries: { staleTime: 60_000, retry: false } },
  });
</script>

<div class="mx-auto grid max-w-5xl gap-8 px-4 py-6">
  <header class="flex flex-wrap items-center justify-between gap-4 border-b pb-4">
    <p class="font-medium">__PROJECT__ health sample</p>
    <nav aria-label="Primary" class="flex flex-wrap gap-4">
      <TextLink href="/">Home</TextLink>
      <TextLink href="/check-ins">Check-ins</TextLink>
      <TextLink href="/auth/development/ava">Development sign-in</TextLink>
    </nav>
  </header>

  <main>
    <QueryClientProvider client={queryClient}>
      {@render children()}
    </QueryClientProvider>
  </main>
</div>
