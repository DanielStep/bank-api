using Bank.Domain;

namespace Bank.Application.Specs;

public class GetAccountsHandlerSpec
{
    public class when_getting_the_accounts
    {
        [Fact]
        public void it_returns_the_repository_accounts_in_order()
        {
            var repository = new FakeAccountRepository(new Accounts(
            [
                new Account(new AccountNumber("2222222222222222"), new Money(20.00m)),
                new Account(new AccountNumber("1111111111111111"), new Money(10.00m))
            ]));

            var accounts = new GetAccountsHandler(repository).Handle(new GetAccountsQuery());

            accounts.All.Select(account => account.Number.Value).ShouldBe(["2222222222222222", "1111111111111111"]);
        }
    }
}
