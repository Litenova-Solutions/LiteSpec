import { test, expect } from "@playwright/test";
import { proves } from "../../src/lib/app/litespec-records";

// The route suite of the __PROJECT__ web app: every address the route tree
// serves, visited in a browser with the use case each address serves named in
// its title. Each test proves the geometry sensor provision, so the suite
// measures every requirement naming the route suite once the audit records it.
const provision = "litespec/core/ui-verification#run-geometry-sensors-on-every-route" as const;

// Each test reads the pages as Ava, signed in through the development
// sign-in, so every route shows what a signed-in reader sees.
test.beforeEach(async ({ page }) => {
  await page.goto("/auth/development/ava");
});

test.describe("route / home leading toward use-case/health.list-check-ins", () => {
  test("renders one heading", proves(provision), async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("h1")).toHaveCount(1);
  });

  test("reflows at 320 CSS pixels without horizontal scrolling", proves(provision), async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await page.goto("/");
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(overflow).toBe(false);
  });

  test("shows a visible focus on the first interactive element", proves(provision), async ({ page }) => {
    await page.goto("/");
    await page.keyboard.press("Tab");
    await expect(page.locator(":focus-visible")).toHaveCount(1);
  });
});

test.describe("route /check-ins serving use-case/health.list-check-ins and use-case/health.record-check-in", () => {
  test("renders one heading", proves(provision), async ({ page }) => {
    await page.goto("/check-ins");
    await expect(page.locator("h1")).toHaveCount(1);
  });

  test("reflows at 320 CSS pixels without horizontal scrolling", proves(provision), async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await page.goto("/check-ins");
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(overflow).toBe(false);
  });

  test("shows a visible focus on the first interactive element", proves(provision), async ({ page }) => {
    await page.goto("/check-ins");
    await page.keyboard.press("Tab");
    await expect(page.locator(":focus-visible")).toHaveCount(1);
  });
});

test.describe("route /check-ins/missing-check-in-1 proving use-case/health.read-check-in", () => {
  test("renders one heading", proves(provision), async ({ page }) => {
    await page.goto("/check-ins/missing-check-in-1");
    await expect(page.locator("h1")).toHaveCount(1);
  });

  test("shows the problem state for an unknown check-in", proves(provision), async ({ page }) => {
    await page.goto("/check-ins/missing-check-in-1");
    await expect(page.getByRole("alert").first()).toBeVisible();
  });

  test("reflows at 320 CSS pixels without horizontal scrolling", proves(provision), async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await page.goto("/check-ins/missing-check-in-1");
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(overflow).toBe(false);
  });

  test("shows a visible focus on the first interactive element", proves(provision), async ({ page }) => {
    await page.goto("/check-ins/missing-check-in-1");
    await page.keyboard.press("Tab");
    await expect(page.locator(":focus-visible")).toHaveCount(1);
  });
});
