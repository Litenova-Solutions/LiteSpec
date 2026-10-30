import { test, expect } from "@playwright/test";
import { proves } from "$lib/app/litespec-records";

test("lists the expenses", proves("scenario/list-expenses.listing-shows-own-expenses"), async ({ page }) => {
  await page.goto("/expenses");
  await expect(page.getByText("spending.receipt-missing")).toBeVisible();
  await expect(page.getByText("Expense submitted")).toBeVisible();
});

test(
  "shows one expense",
  {
    annotation: [
      { type: "proves", description: "scenario/get-expense.detail-shows-expense" },
      { type: "journey", description: "journey/submit-and-reimburse" },
    ],
  },
  async ({ page }) => {
    const id = "9";
    const detail = page.getByRole("heading");
    await page.goto(`/expenses/${id}`);
    await expect(page.getByText("expenses.not-found")).toBeVisible();
    await expect(detail).toContainText("Expense detail");
  },
);
