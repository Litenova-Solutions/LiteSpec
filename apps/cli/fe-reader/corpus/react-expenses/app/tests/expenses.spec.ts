import { test, expect } from "@playwright/test";
import { proves } from "@/app/litespec-records";

test("lists the expenses", proves("scenario/list-expenses.listing-shows-own-expenses"), async ({ page }) => {
  await page.goto("/expenses");
  await expect(page.getByText("spending.receipt-missing")).toBeVisible();
});
