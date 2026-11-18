<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/web.sign-in" } as const satisfies LiteSpecMark;
</script>

<script>
  import { goto } from "$app/navigation";

  const dynamicTarget = "/auth/login";

  function startLogin(next: string) {
    window.location.href = "/auth/login?next=/home";
    goto("/auth/login");
    goto(`/auth/callback?code=${next}`);
    goto(dynamicTarget);
  }
</script>

<h1>Sign in</h1>
<a href="/auth/login">Sign in with the host</a>
<a href={dynamicTarget}>Dynamic target</a>
<form action="/auth/login">
  <button type="submit">Go</button>
</form>
