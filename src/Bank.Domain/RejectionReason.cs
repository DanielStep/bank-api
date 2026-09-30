namespace Bank.Domain;

// Declared in precedence order: a Transfer is Rejected with the first reason that applies.
public enum RejectionReason
{
    NonPositiveAmount,
    SameAccount,
    UnknownSendingAccount,
    UnknownReceivingAccount,
    InsufficientFunds
}
