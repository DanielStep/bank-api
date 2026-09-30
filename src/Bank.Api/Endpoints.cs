using Bank.Application;
using Bank.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api;

public class Endpoints
{
    public async Task<IResult> PostTransferBatch(HttpRequest request, SettleTransferBatchHandler handler)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest();

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file == null)
            return Results.BadRequest();

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
