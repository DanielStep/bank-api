using Bank.Domain;

namespace Bank.Application.Specs;

public class SettleTransferBatchHandlerSpec
{
    static Accounts TwoAccounts() => new(
    [
        new Account(new AccountNumber("1111111111111111"), new Money(100.00m)),
        new Account(new AccountNumber("2222222222222222"), new Money(0.00m))
    ]);

    public class when_the_csv_is_malformed
    {
        readonly FakeAccountRepository repository = new(TwoAccounts());
        readonly SettleTransferBatchOutcome outcome;

        public when_the_csv_is_malformed()
        {
            outcome = new SettleTransferBatchHandler(repository)
                .Handle(new SettleTransferBatchCommand("1111111111111111,2222222222222222\n"));
        }

        [Fact]
        public void it_returns_the_errors() =>
            outcome.Errors.Select(error => error.Line).ShouldBe([1]);

        [Fact]
        public void it_returns_no_result() =>
            outcome.Result.ShouldBeNull();

        [Fact]
        public void it_saves_nothing() =>
            repository.SaveCount.ShouldBe(0);
    }

    public class when_the_csv_is_well_formed
    {
        readonly FakeAccountRepository repository = new(TwoAccounts());
        readonly SettleTransferBatchOutcome outcome;

        public when_the_csv_is_well_formed()
        {
            outcome = new SettleTransferBatchHandler(repository)
                .Handle(new SettleTransferBatchCommand("1111111111111111,2222222222222222,30.00\n2222222222222222,1111111111111111,10.00\n"));
        }

        [Fact]
        public void it_maps_each_line_to_the_position_of_a_transfer() =>
            outcome.Result!.Settled.Select(transfer => transfer.Position).ShouldBe([1, 2]);

        [Fact]
        public void it_maps_from_and_to_to_the_sending_and_receiving_account()
        {
            var transfer = outcome.Result!.Settled[0];

            transfer.SendingAccount.Value.ShouldBe("1111111111111111");
            transfer.ReceivingAccount.Value.ShouldBe("2222222222222222");
            transfer.Requested.ShouldBe(30.00m);
        }

        [Fact]
        public void it_settles_the_batch_against_the_loaded_accounts() =>
            outcome.Result!.Accounts.Find(new AccountNumber("1111111111111111"))!.Balance.Value.ShouldBe(80.00m);

        [Fact]
        public void it_saves_the_resulting_accounts_once()
        {
            repository.SaveCount.ShouldBe(1);
            repository.Saved.ShouldBe(outcome.Result!.Accounts);
        }

        [Fact]
        public void it_returns_no_errors() =>
            outcome.Errors.ShouldBeEmpty();
    }
}
