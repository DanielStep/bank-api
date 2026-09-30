namespace Bank.Domain;

public class Transfer
{
    public int Position { get; }
    public AccountNumber SendingAccount { get; }
    public AccountNumber ReceivingAccount { get; }
    public decimal Requested { get; }
    public Money? Amount { get; }
    public TransferStatus Status { get; private set; }
    public RejectionReason? Reason { get; private set; }

    public Transfer(int position, AccountNumber sendingAccount, AccountNumber receivingAccount, decimal requested)
    {
        Position = position;
        SendingAccount = sendingAccount;
        ReceivingAccount = receivingAccount;
        Requested = requested;
        Status = TransferStatus.Unsettled;

        if (requested <= 0)
            Reject(RejectionReason.NonPositiveAmount);
        else if (sendingAccount == receivingAccount)
            Reject(RejectionReason.SameAccount);
        else
            Amount = new Money(requested);
    }

    public void SettleBetween(Account sending, Account receiving)
    {
        if (Amount == null)
            throw new InvalidOperationException("A Rejected Transfer cannot be settled.");

        if (!sending.TryWithdraw(Amount.Value))
            return;

        receiving.Deposit(Amount.Value);
        Status = TransferStatus.Settled;
    }

    internal void Reject(RejectionReason reason)
    {
        Status = TransferStatus.Rejected;
        Reason = reason;
    }
}
