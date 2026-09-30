namespace Bank.Domain;

public class TransferBatch
{
    public IReadOnlyList<Transfer> Transfers { get; }

    public TransferBatch(IEnumerable<Transfer> transfers)
    {
        Transfers = transfers.OrderBy(transfer => transfer.Position).ToList();
    }

    public SettlementResult Settle(Accounts accounts)
    {
        var settled = new List<Transfer>();

        bool settledSomething;
        do
        {
            settledSomething = false;

            foreach (var transfer in Transfers)
            {
                if (transfer.Status == TransferStatus.Unsettled && TrySettle(transfer, accounts))
                {
                    settled.Add(transfer);
                    settledSomething = true;
                }
            }
        }
        while (settledSomething);

        foreach (var transfer in Transfers)
        {
            if (transfer.Status == TransferStatus.Unsettled)
                transfer.Reject(RejectionReason.InsufficientFunds);
        }

        var rejected = Transfers.Where(transfer => transfer.Status == TransferStatus.Rejected).ToList();
        return new SettlementResult(settled, rejected, accounts);
    }

    private bool TrySettle(Transfer transfer, Accounts accounts)
    {
        var sending = accounts.Find(transfer.SendingAccount);
        if (sending == null)
        {
            transfer.Reject(RejectionReason.UnknownSendingAccount);
            return false;
        }

        var receiving = accounts.Find(transfer.ReceivingAccount);
        if (receiving == null)
        {
            transfer.Reject(RejectionReason.UnknownReceivingAccount);
            return false;
        }

        transfer.SettleBetween(sending, receiving);
        return transfer.Status == TransferStatus.Settled;
    }
}
