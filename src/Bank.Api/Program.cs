using Bank.Api;
using Bank.Application;
using Bank.Data;
using Bank.Domain;

var builder = WebApplication.CreateBuilder(args);

// The build output holds a working copy of the repo's balances file, so the repo's file is never written.
var balancesPath = Path.Combine(AppContext.BaseDirectory, "mable_account_balances.csv");
builder.Services.AddSingleton<IAccountRepository>(new FileAccountRepository(balancesPath));
builder.Services.AddSingleton<SettleTransferBatchHandler>();
builder.Services.AddSingleton<GetAccountsHandler>();

var app = builder.Build();

// Refuse to start on a balances file that can't be trusted.
app.Services.GetRequiredService<IAccountRepository>().GetAll();

var endpoints = new Endpoints();
app.MapPost("/transfer-batches", endpoints.PostTransferBatch).DisableAntiforgery();
app.MapGet("/accounts", endpoints.GetAccounts);

app.Run();
