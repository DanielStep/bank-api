namespace Bank.Domain;

public enum RejectionReason
{
    NonPositiveAmount,
    SameAccount,
    UnknownSendingAccount,
    UnknownReceivingAccount,
    InsufficientFunds
}
