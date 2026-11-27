<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/claimwick-app.expense-record" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { page } from "$app/state";
  import {
    approveExpense,
    attachReceipt,
    client,
    refuseExpense,
    reimburseExpense,
    type ExpenseRow,
    type ListExpensesResponse,
  } from "@claimwick/api";
  import { ListPage } from "@claimwick/ui/floorplans";
  import { formatMoney } from "#lib/app/ui/money.ts";
  import { sentenceFor } from "./refusals.ts";

  const expenseId = page.params.expenseId ?? "";

  let expense = $state<ExpenseRow | null>(null);
  let loading = $state(true);
  let loadFailed = $state(false);

  let reason = $state("");
  let attachFiles = $state<FileList | undefined>(undefined);

  let approving = $state(false);
  let refusing = $state(false);
  let attaching = $state(false);
  let reimbursing = $state(false);

  let approvalNotice = $state<string | null>(null);
  let refusalNotice = $state<string | null>(null);
  let attachNotice = $state<string | null>(null);
  let reimburseNotice = $state<string | null>(null);

  async function loadExpense(): Promise<void> {
    loading = true;
    loadFailed = false;
    try {
      // The list read fills the facts but is not an offered use case, so it
      // goes through the client directly instead of the generated list function.
      const result = await client.get<ListExpensesResponse, unknown, true>({
        url: "/api/expenses",
        throwOnError: true,
      });
      expense = result.expenses.find((row) => row.expenseId === expenseId) ?? null;
    } catch {
      loadFailed = true;
    } finally {
      loading = false;
    }
  }

  void loadExpense();

  async function approve(): Promise<void> {
    approving = true;
    approvalNotice = null;
    try {
      await approveExpense({ throwOnError: true, path: { expenseId } });
      await loadExpense();
    } catch (error) {
      approvalNotice = sentenceFor(
        "approve",
        error,
        "The expense could not be approved. Try again in a moment.",
      );
    } finally {
      approving = false;
    }
  }

  async function refuse(): Promise<void> {
    refusing = true;
    refusalNotice = null;
    try {
      await refuseExpense({ throwOnError: true, path: { expenseId }, query: { reason } });
      await loadExpense();
    } catch (error) {
      refusalNotice = sentenceFor(
        "refuse",
        error,
        "The expense could not be refused. Try again in a moment.",
      );
    } finally {
      refusing = false;
    }
  }

  async function attach(): Promise<void> {
    if ((attachFiles?.length ?? 0) === 0) {
      return;
    }
    attaching = true;
    attachNotice = null;
    try {
      const receiptId = crypto.randomUUID();
      await attachReceipt({ throwOnError: true, query: { receiptId, expenseId } });
      await loadExpense();
    } catch (error) {
      attachNotice = sentenceFor(
        "attach",
        error,
        "The receipt could not be attached. Try again in a moment.",
      );
    } finally {
      attaching = false;
    }
  }

  async function reimburse(): Promise<void> {
    reimbursing = true;
    reimburseNotice = null;
    try {
      await reimburseExpense({ throwOnError: true, path: { expenseId } });
      await loadExpense();
    } catch (error) {
      reimburseNotice = sentenceFor(
        "reimburse",
        error,
        "The expense could not be reimbursed. Try again in a moment.",
      );
    } finally {
      reimbursing = false;
    }
  }

  const erasureHref = `/expenses/${expenseId}/erasure`;</script>

<ListPage title={expenseId}>
  <nav aria-label="Breadcrumb">
    <a href="/expenses">Expenses</a>
    <span aria-current="page">{expenseId}</span>
  </nav>

  {#if loading}
    <p role="status">Loading expense.</p>
  {:else if loadFailed}
    <p role="alert">The expense could not be loaded. Try again in a moment.</p>
  {:else if expense === null}
    <p role="alert">This expense does not exist.</p>
    <p><a href="/expenses">Back to expenses.</a></p>
  {:else}
    <p>Status: {expense.state}</p>
    <p>Filed {expense.submittedAt}.</p>

    <section aria-label="The expense">
      <h2>The expense</h2>
      <p>Amount: {formatMoney(expense)}</p>
    </section>

    {#if expense.state === "Submitted"}
      <section aria-label="Decision">
        <h2>Decision</h2>
        <button onclick={() => void approve()} disabled={approving}>
          {approving ? "Approving expense." : "Approve Expense"}
        </button>
        {#if approvalNotice}
          <p role="alert">{approvalNotice}</p>
        {/if}

        <form
          onsubmit={(event) => {
            event.preventDefault();
            void refuse();
          }}
        >
          <label for="refuse-reason">Reason</label>
          <input id="refuse-reason" name="reason" type="text" required bind:value={reason} />
          <button type="submit" disabled={refusing}>
            {refusing ? "Refusing expense." : "Refuse Expense"}
          </button>
        </form>
        {#if refusalNotice}
          <p role="alert">{refusalNotice}</p>
        {/if}

        <form
          onsubmit={(event) => {
            event.preventDefault();
            void attach();
          }}
        >
          <label for="receipt">Receipt</label>
          <input
            id="receipt"
            name="receipt"
            type="file"
            required
            accept="application/pdf,image/jpeg,image/png"
            bind:files={attachFiles}
          />
          <button type="submit" disabled={attaching}>
            {attaching ? "Attaching receipt." : "Attach Receipt"}
          </button>
        </form>
        {#if attachNotice}
          <p role="alert">{attachNotice}</p>
        {/if}
      </section>
    {/if}

    {#if expense.state === "Approved"}
      <section aria-label="Reimbursement">
        <h2>Reimbursement</h2>
        <button onclick={() => void reimburse()} disabled={reimbursing}>
          {reimbursing ? "Reimbursing expense." : "Reimburse Expense"}
        </button>
        {#if reimburseNotice}
          <p role="alert">{reimburseNotice}</p>
        {/if}
      </section>
    {/if}

    <section aria-label="Personal data">
      <h2>Personal data</h2>
      <p><a href={erasureHref}>Erase personal data</a></p>
    </section>
  {/if}
</ListPage>
