using Claimwick.Platform.Http;
using Claimwick.Platform.Modules;
using Claimwick.Reporting;
using Claimwick.Spending;
using Claimwick.Travel;
using Claimwick.Travel.Germany;
using Claimwick.Travel.Netherlands;
using Claimwick.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlatform();
builder.Services.AddTravel(travel => travel.Add<Germany>().Add<Netherlands>());
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
