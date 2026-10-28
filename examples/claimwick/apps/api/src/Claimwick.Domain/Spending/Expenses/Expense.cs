using Claimwick.Domain.Spending.Expenses.Events;
using Claimwick.Domain.Spending.Expenses.Exceptions;
using Claimwick.Domain.Spending.Receipts.Events;
using Claimwick.Domain.Spending.Receipts.Exceptions;

namespace Claimwick.Domain.Spending.Expenses;

/// <summary>One work expense an employee submitted for approval and reimbursement.</summary>
/// <implements>aggregate/spending.expense</implements>
public sealed class Expense : AggregateRoot<Guid>
{
    private Expense(Guid id, Guid employeeId, Money total, Guid? receiptId)
    {
        Id = id;
        EmployeeId = employeeId;
        Total = total;
        ReceiptId = receiptId;
        State = new ExpenseSubmittedState();
    }

    /// <summary>The expense identity.</summary>
    public Guid Id { get; }

    /// <summary>The employee who submitted the expense.</summary>
    public Guid EmployeeId { get; }

    /// <summary>The exact amount with its currency.</summary>
    public Money Total { get; }

    /// <summary>The attached receipt, when one is attached.</summary>
    public Guid? ReceiptId { get; private set; }

    /// <summary>The current lifecycle state.</summary>
    public ExpenseState State { get; private set; }

    /// <summary>Submits one expense, refusing one above the receipt threshold that carries no receipt.</summary>
    /// <implements>use-case/spending.submit-expense</implements>
    public static Expense Submit(Guid id, Guid employeeId, Money total, Guid? receiptId, Money receiptThreshold)
    {
        if (receiptId is null && total.Amount > receiptThreshold.Amount)
        {
            throw new ExpenseReceiptMissingException();
        }

        var expense = new Expense(id, employeeId, total, receiptId);
        expense.RaiseDomainEvent(new ExpenseSubmittedEvent(id, total.Amount, total.Currency, employeeId));
        return expense;
    }

    /// <summary>Attaches one receipt to a submitted expense.</summary>
    /// <implements>use-case/spending.attach-receipt</implements>
    public void AttachReceipt(Guid receiptId)
    {
        if (State is not ExpenseSubmittedState)
        {
            throw new ReceiptExpenseDecidedException();
        }

        ReceiptId = receiptId;
        RaiseDomainEvent(new ReceiptAttachedEvent(receiptId, Id));
    }

    /// <summary>Approves a submitted expense from another employee.</summary>
    /// <implements>use-case/spending.approve-expense</implements>
    public void Approve(Guid approverId, DateTimeOffset decidedAt)
    {
        if (approverId == EmployeeId)
        {
            throw new ExpenseApproverIsSubmitterException();
        }

        if (State is not ExpenseSubmittedState)
        {
            throw new ExpenseNotSubmittedException();
        }

        State = new ExpenseApprovedState(approverId, decidedAt);
        RaiseDomainEvent(new ExpenseApprovedEvent(Id, approverId, decidedAt));
    }

    /// <summary>Refuses a submitted expense with a reason.</summary>
    /// <implements>use-case/spending.refuse-expense</implements>
    public void Refuse(Guid approverId, string reason, DateTimeOffset decidedAt)
    {
        if (State is not ExpenseSubmittedState)
        {
            throw new ExpenseNotSubmittedException();
        }

        State = new ExpenseRefusedState(approverId, decidedAt, reason);
        RaiseDomainEvent(new ExpenseRefusedEvent(Id, approverId, decidedAt));
    }

    /// <summary>Reimburses an approved expense once.</summary>
    /// <implements>use-case/spending.reimburse-expense</implements>
    public void Reimburse(DateTimeOffset reimbursedAt)
    {
        if (State is ExpenseReimbursedState)
        {
            throw new ExpenseAlreadyReimbursedException();
        }

        if (State is not ExpenseApprovedState)
        {
            throw new ExpenseNotApprovedException();
        }

        State = new ExpenseReimbursedState(reimbursedAt);
        RaiseDomainEvent(new ExpenseReimbursedEvent(Id, Total.Amount, Total.Currency, reimbursedAt));
    }

    /// <summary>Erases the personal data of a decided expense past its retention.</summary>
    /// <implements>use-case/spending.erase-personal-data</implements>
    public void Erase()
    {
        ReceiptId = null;
    }

    /// <summary>Refuses an export with no approved expense.</summary>
    public static void RequireApprovedForExport(int approvedCount)
    {
        if (approvedCount == 0)
        {
            throw new ExpenseNothingApprovedException();
        }
    }
}
