namespace Bank.Domain.Specs;

public class AccountSpec
{
    static Account AnAccountHolding(decimal balance) =>
        new(new AccountNumber("1111111111111111"), new Money(balance));

    public class when_withdrawing_the_whole_balance
    {
        readonly Account account = AnAccountHolding(100.00m);

        [Fact]
        public void it_succeeds() =>
            account.TryWithdraw(new Money(100.00m)).ShouldBeTrue();

        [Fact]
        public void it_leaves_zero()
        {
            account.TryWithdraw(new Money(100.00m));
            account.Balance.ShouldBe(new Money(0.00m));
        }
    }

    public class when_withdrawing_one_cent_more_than_the_balance
    {
        readonly Account account = AnAccountHolding(99.99m);

        [Fact]
        public void it_fails() =>
            account.TryWithdraw(new Money(100.00m)).ShouldBeFalse();

        [Fact]
        public void it_leaves_the_balance_unchanged()
        {
            account.TryWithdraw(new Money(100.00m));
            account.Balance.ShouldBe(new Money(99.99m));
        }
    }
}
