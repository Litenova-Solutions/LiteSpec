import { test, expect } from "@playwright/test";
import { proves } from "../../src/lib/app/litespec-records";

test(
  "an employee sees their own expenses on the expense list",
  proves("scenario/list-expenses.listing-own-expenses-succeeds"),
  async ({ page }) => {
    await page.goto("/expenses");
    await expect(page.getByRole("table", { name: "Your expenses, newest first" })).toBeVisible();
  },
);
