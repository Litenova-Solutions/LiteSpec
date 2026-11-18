import type { LiteSpecMark } from "@/app/litespec-records";

export const litespec = { implements: "screen/web.sign-in" } as const satisfies LiteSpecMark;

export function SignInPage(next: string): unknown {
  window.location.href = "/auth/login?next=/home";
  location.assign(`/auth/callback?code=${next}`);
  const target = "/auth/login";
  location.replace(target);
  return (
    <main>
      <a href="/auth/login">Sign in</a>
      <a href={target}>Dynamic target</a>
      <form action="/auth/login">
        <button type="submit">Go</button>
      </form>
    </main>
  );
}
