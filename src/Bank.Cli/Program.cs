using Bank.Application;
using Bank.Cli;
using Bank.Data;
using Bank.Domain;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Bank.Cli <balances.csv> <transfers.csv>");
    return 1;
}

var services = new ServiceCollection();
services.AddSingleton<IAccountRepository>(new FileAccountRepository(args[0]));
services.AddSingleton<SettleTransferBatchHandler>();
services.AddSingleton(provider => new SettlementJob(provider.GetRequiredService<SettleTransferBatchHandler>(), Console.Out, Console.Error));

using var provider = services.BuildServiceProvider();
return provider.GetRequiredService<SettlementJob>().Run(args[0], args[1]);
