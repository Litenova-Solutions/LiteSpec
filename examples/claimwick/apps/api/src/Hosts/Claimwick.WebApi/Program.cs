using Claimwick.Platform.Http;
using Claimwick.Platform.Modules;
using Claimwick.Reporting;
using Claimwick.Spending;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlatform();
builder.Services.AddModules(builder.Configuration, new SpendingModule(), new ReportingModule());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

public partial class Program;
