using System.Text.Json;
using Bank.Application;
using Bank.Data;
using Bank.Domain;

namespace Bank.Cli;

public class SettlementJob
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly TextWriter output;
    private readonly TextWriter error;

    public SettlementJob(TextWriter output, TextWriter error)
    {
        this.output = output;
        this.error = error;
    }

    public int Run(string[] args)
    {
        if (args.Length != 2)
        {
            error.WriteLine("Usage: Bank.Cli <balances.csv> <transfers.csv>");
            return 1;
        }

        try
        {
            return Settle(args[0], args[1]);
        }
        catch (Exception e) when (e is IOException or InvalidDataException)
        {
            error.WriteLine(e.Message);
            return 1;
        }
    }

    private int Settle(string balancesPath, string transfersPath)
    {
        var handler = new SettleTransferBatchHandler(new FileAccountRepository(balancesPath));
        var outcome = handler.Handle(new SettleTransferBatchCommand(File.ReadAllText(transfersPath)));
        if (outcome.Errors.Count > 0)
        {
            error.WriteLine($"{transfersPath} is malformed:");
            foreach (var csvError in outcome.Errors)
                error.WriteLine($"line {csvError.Line}: {csvError.Message}");

            return 1;
        }

        output.WriteLine(JsonSerializer.Serialize(ToReport(outcome.Result!), JsonOptions));
        output.WriteLine();
        output.WriteLine($"Balances in {balancesPath}:");
        output.Write(File.ReadAllText(balancesPath));
        return 0;
    }

    private SettlementReport ToReport(SettlementResult result)
    {
        var settled = new List<TransferLine>();
        foreach (var transfer in result.Settled)
            settled.Add(new TransferLine(transfer.Position, transfer.SendingAccount.Value, transfer.ReceivingAccount.Value, transfer.Requested));

        var rejected = new List<RejectedLine>();
        foreach (var transfer in result.Rejected)
            rejected.Add(new RejectedLine(transfer.Position, transfer.SendingAccount.Value, transfer.ReceivingAccount.Value, transfer.Requested, transfer.Reason.ToString()!));

        var balances = new List<BalanceLine>();
        foreach (var account in result.Accounts.All)
            balances.Add(new BalanceLine(account.Number.Value, account.Balance.Value));

        return new SettlementReport(settled, rejected, balances);
    }
}
