using Bank.Domain;

namespace Bank.Application;

public class GetAccountsHandler
{
    private readonly IAccountRepository repository;

    public GetAccountsHandler(IAccountRepository repository)
    {
        this.repository = repository;
    }

    public Accounts Handle(GetAccountsQuery query)
    {
        return repository.GetAll();
    }
}
