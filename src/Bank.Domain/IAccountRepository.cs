namespace Bank.Domain;

public interface IAccountRepository
{
    Accounts GetAll();

    void SaveAll(Accounts accounts);
}
