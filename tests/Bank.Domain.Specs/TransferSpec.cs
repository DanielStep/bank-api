namespace Bank.Domain.Specs;

public class TransferSpec
{
    static Transfer ATransfer(string sending, string receiving, decimal requested) =>
        new(1, new AccountNumber(sending), new AccountNumber(receiving), requested);

    public class when_created_with_an_amount_of_zero_or_less
    {
        [Fact]
        public void it_is_rejected_for_zero()
        {
            var transfer = ATransfer("1111111111111111", "2222222222222222", 0.00m);

            transfer.Status.ShouldBe(TransferStatus.Rejected);
            transfer.Reason.ShouldBe(RejectionReason.NonPositiveAmount);
        }

        [Fact]
        public void it_is_rejected_for_a_negative_amount()
        {
            var transfer = ATransfer("1111111111111111", "2222222222222222", -5.00m);

            transfer.Status.ShouldBe(TransferStatus.Rejected);
            transfer.Reason.ShouldBe(RejectionReason.NonPositiveAmount);
        }

        [Fact]
        public void it_keeps_the_requested_amount() =>
            ATransfer("1111111111111111", "2222222222222222", -5.00m).Requested.ShouldBe(-5.00m);
    }

    public class when_created_with_the_same_sending_and_receiving_account
    {
        [Fact]
        public void it_is_rejected()
        {
            var transfer = ATransfer("1111111111111111", "1111111111111111", 10.00m);

            transfer.Status.ShouldBe(TransferStatus.Rejected);
            transfer.Reason.ShouldBe(RejectionReason.SameAccount);
        }
    }

    public class when_created_with_a_negative_amount_and_the_same_account
    {
        [Fact]
        public void it_is_rejected_as_non_positive() =>
            ATransfer("1111111111111111", "1111111111111111", -5.00m).Reason.ShouldBe(RejectionReason.NonPositiveAmount);
    }

    public class when_created_with_a_positive_amount_between_two_accounts
    {
        readonly Transfer transfer = ATransfer("1111111111111111", "2222222222222222", 120.50m);

        [Fact]
        public void it_is_unsettled()
        {
            transfer.Status.ShouldBe(TransferStatus.Unsettled);
            transfer.Reason.ShouldBeNull();
        }

        [Fact]
        public void it_holds_the_amount_as_money() =>
            transfer.Amount.ShouldBe(new Money(120.50m));
    }

    public class when_settled_and_the_sending_account_holds_enough
    {
        readonly Transfer transfer = ATransfer("1111111111111111", "2222222222222222", 100.00m);
        readonly Account sending = new(new AccountNumber("1111111111111111"), new Money(100.00m));
        readonly Account receiving = new(new AccountNumber("2222222222222222"), new Money(0.00m));

        public when_settled_and_the_sending_account_holds_enough()
        {
            transfer.SettleBetween(sending, receiving);
        }

        [Fact]
        public void it_moves_the_amount()
        {
            sending.Balance.ShouldBe(new Money(0.00m));
            receiving.Balance.ShouldBe(new Money(100.00m));
        }

        [Fact]
        public void it_is_settled() =>
            transfer.Status.ShouldBe(TransferStatus.Settled);
    }

    public class when_settled_and_the_sending_account_is_short
    {
        readonly Transfer transfer = ATransfer("1111111111111111", "2222222222222222", 100.00m);
        readonly Account sending = new(new AccountNumber("1111111111111111"), new Money(99.99m));
        readonly Account receiving = new(new AccountNumber("2222222222222222"), new Money(0.00m));

        public when_settled_and_the_sending_account_is_short()
        {
            transfer.SettleBetween(sending, receiving);
        }

        [Fact]
        public void it_moves_nothing()
        {
            sending.Balance.ShouldBe(new Money(99.99m));
            receiving.Balance.ShouldBe(new Money(0.00m));
        }

        [Fact]
        public void it_stays_unsettled() =>
            transfer.Status.ShouldBe(TransferStatus.Unsettled);
    }
}
