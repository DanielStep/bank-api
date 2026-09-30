using Bank.Application;
using Bank.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api;

public class Endpoints
{
    public IResult PostTransferBatch(IFormFile file, SettleTransferBatchHandler handler)
    {
        string csv;
        using (var reader = new StreamReader(file.OpenReadStream()))
            csv = reader.ReadToEnd();

        var outcome = handler.Handle(new SettleTransferBatchCommand(csv));
        if (outcome.Errors.Count > 0)
        {
            var problem = new ProblemDetails();
            problem.Status = 400;
            problem.Title = "The CSV file is malformed";
            problem.Extensions["errors"] = outcome.Errors;
            return Results.Problem(problem);
        }

        return Results.Ok(ToResponse(outcome.Result!));
    }

    public List<BalanceLine> GetAccounts(GetAccountsHandler handler)
    {
        return ToBalanceLines(handler.Handle(new GetAccountsQuery()));
    }

    private TransferBatchResponse ToResponse(SettlementResult result)
    {
        var settled = new List<TransferLine>();
        foreach (var transfer in result.Settled)
            settled.Add(new TransferLine(transfer.Position, transfer.SendingAccount.Value, transfer.ReceivingAccount.Value, transfer.Requested));

        var rejected = new List<RejectedLine>();
        foreach (var transfer in result.Rejected)
            rejected.Add(new RejectedLine(transfer.Position, transfer.SendingAccount.Value, transfer.ReceivingAccount.Value, transfer.Requested, transfer.Reason.ToString()!));

        return new TransferBatchResponse(settled, rejected, ToBalanceLines(result.Accounts));
    }

    private List<BalanceLine> ToBalanceLines(Accounts accounts)
    {
        var lines = new List<BalanceLine>();
        foreach (var account in accounts.All)
            lines.Add(new BalanceLine(account.Number.Value, account.Balance.Value));

        return lines;
    }
}
