using Claimwick.Platform.Modules;
using Claimwick.Platform.Persistence;
using Claimwick.Spending.Domain.Expenses;
using Claimwick.Spending.Domain.Policies;
using Claimwick.Spending.Domain.ReminderPreferences;
using Claimwick.Spending.Infrastructure.Persistence;
using Claimwick.Spending.Application.Notifications.ApprovalReminder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Spending;

/// <summary>The Spending module: expenses, policies, receipts and reminder preferences with their use cases.</summary>
public sealed class SpendingModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SpendingOptions>(configuration.GetSection(SpendingOptions.SectionName));
        services.AddSingleton<IStoreContribution, SpendingStoreContribution>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IReminderPreferenceRepository, ReminderPreferenceRepository>();
    }
}
