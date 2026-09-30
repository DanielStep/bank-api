namespace Bank.Domain;

public class Account
{
    public AccountNumber Number { get; }

    public Money Balance { get; private set; }

    public Account(AccountNumber number, Money balance)
    {
        Number = number;
        Balance = balance;
    }

    public bool TryWithdraw(Money amount)
    {
        if (amount.Value > Balance.Value)
            return false;

        Balance = Balance.Subtract(amount);
        return true;
    }

    public void Deposit(Money amount)
    {
        Balance = Balance.Add(amount);
    }
}
