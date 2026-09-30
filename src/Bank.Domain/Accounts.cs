namespace Bank.Domain;

public class Accounts
{
    private readonly List<Account> all = new();
    private readonly Dictionary<AccountNumber, Account> byNumber = new();

    public Accounts(IEnumerable<Account> accounts)
    {
        foreach (var account in accounts)
        {
            if (byNumber.ContainsKey(account.Number))
                throw new ArgumentException($"Account {account.Number} is listed more than once.");

            byNumber.Add(account.Number, account);
            all.Add(account);
        }
    }

    public IReadOnlyList<Account> All => all;

    public Account? Find(AccountNumber number)
    {
        if (byNumber.TryGetValue(number, out var account))
            return account;

        return null;
    }
}
