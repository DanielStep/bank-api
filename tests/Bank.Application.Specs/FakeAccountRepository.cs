using Bank.Domain;

namespace Bank.Application.Specs;

// Holds the Accounts in memory and records what the handler saves.
class FakeAccountRepository : IAccountRepository
{
    private readonly Accounts accounts;

    public int SaveCount { get; private set; }
    public Accounts? Saved { get; private set; }

    public FakeAccountRepository(Accounts accounts)
    {
        this.accounts = accounts;
    }

    public Accounts GetAll() => accounts;

    public void SaveAll(Accounts accounts)
    {
        SaveCount++;
        Saved = accounts;
    }
}
