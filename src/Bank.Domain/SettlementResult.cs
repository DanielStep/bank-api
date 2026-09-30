namespace Bank.Domain;

public class SettlementResult
{
    public IReadOnlyList<Transfer> Settled { get; }
    public IReadOnlyList<Transfer> Rejected { get; }
    public Accounts Accounts { get; }

    public SettlementResult(IReadOnlyList<Transfer> settled, IReadOnlyList<Transfer> rejected, Accounts accounts)
    {
        Settled = settled;
        Rejected = rejected;
        Accounts = accounts;
    }
}
