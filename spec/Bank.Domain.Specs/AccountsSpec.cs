namespace Bank.Domain.Specs;

public class AccountsSpec
{
    static Account AnAccount(string number) =>
        new(new AccountNumber(number), new Money(10.00m));

    public class when_finding_an_account
    {
        readonly Account account = AnAccount("1111111111111111");
        readonly Accounts accounts;

        public when_finding_an_account()
        {
            accounts = new Accounts([account]);
        }

        [Fact]
        public void it_returns_a_known_account() =>
            accounts.Find(new AccountNumber("1111111111111111")).ShouldBe(account);

        [Fact]
        public void it_returns_null_for_an_unknown_account() =>
            accounts.Find(new AccountNumber("9999999999999999")).ShouldBeNull();
    }

    public class when_listing_all_accounts
    {
        [Fact]
        public void it_keeps_the_order_it_was_given()
        {
            var accounts = new Accounts([AnAccount("3333333333333333"), AnAccount("1111111111111111"), AnAccount("2222222222222222")]);

            accounts.All.Select(account => account.Number.Value)
                .ShouldBe(["3333333333333333", "1111111111111111", "2222222222222222"]);
        }
    }

    public class when_given_the_same_account_number_twice
    {
        [Fact]
        public void it_is_refused() =>
            Should.Throw<ArgumentException>(() => new Accounts([AnAccount("1111111111111111"), AnAccount("1111111111111111")]))
                .Message.ShouldContain("1111111111111111");
    }
}
