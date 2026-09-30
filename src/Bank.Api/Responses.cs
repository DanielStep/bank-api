namespace Bank.Api;

public record TransferBatchResponse(List<TransferLine> Settled, List<RejectedLine> Rejected, List<BalanceLine> Balances);

public record TransferLine(int Line, string From, string To, decimal Amount);

public record RejectedLine(int Line, string From, string To, decimal Amount, string Reason);

public record BalanceLine(string AccountNumber, decimal Balance);
