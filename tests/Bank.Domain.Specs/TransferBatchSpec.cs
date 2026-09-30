namespace Bank.Domain.Specs;

public class TransferBatchSpec
{
    static Account AnAccount(string number, decimal balance) =>
        new(new AccountNumber(number), new Money(balance));

    static Transfer ATransfer(int position, string sending, string receiving, decimal amount) =>
        new(position, new AccountNumber(sending), new AccountNumber(receiving), amount);

    static SettlementResult Settle(Account[] accounts, Transfer[] transfers) =>
        new TransferBatch(transfers).Settle(new Accounts(accounts));

    static decimal BalanceOf(SettlementResult result, string number) =>
        result.Accounts.Find(new AccountNumber(number))!.Balance.Value;

    static Transfer At(SettlementResult result, int position) =>
        result.Settled.Concat(result.Rejected).Single(transfer => transfer.Position == position);

    static int[] SettledPositions(SettlementResult result) =>
        result.Settled.Select(transfer => transfer.Position).ToArray();

    public class when_the_sending_account_holds_enough
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 500.00m), AnAccount("2222222222222222", 100.00m)],
            [ATransfer(1, "1111111111111111", "2222222222222222", 120.50m)]);

        [Fact]
        public void it_settles_the_transfer() =>
            At(result, 1).Status.ShouldBe(TransferStatus.Settled);

        [Fact]
        public void it_moves_the_amount()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(379.50m);
            BalanceOf(result, "2222222222222222").ShouldBe(220.50m);
        }
    }

    public class when_the_sending_account_is_emptied_exactly
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 100.00m), AnAccount("2222222222222222", 0.00m)],
            [ATransfer(1, "1111111111111111", "2222222222222222", 100.00m)]);

        [Fact]
        public void it_settles_the_transfer() =>
            At(result, 1).Status.ShouldBe(TransferStatus.Settled);

        [Fact]
        public void it_leaves_the_sending_account_at_zero()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(0.00m);
            BalanceOf(result, "2222222222222222").ShouldBe(100.00m);
        }
    }

    public class when_the_sending_account_is_one_cent_short
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 99.99m), AnAccount("2222222222222222", 0.00m)],
            [ATransfer(1, "1111111111111111", "2222222222222222", 100.00m)]);

        [Fact]
        public void it_rejects_the_transfer_for_insufficient_funds() =>
            At(result, 1).Reason.ShouldBe(RejectionReason.InsufficientFunds);

        [Fact]
        public void it_moves_nothing()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(99.99m);
            BalanceOf(result, "2222222222222222").ShouldBe(0.00m);
        }
    }

    public class when_an_earlier_transfer_funds_a_later_one
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 100.00m), AnAccount("2222222222222222", 0.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "2222222222222222", "3333333333333333", 100.00m)
            ]);

        [Fact]
        public void it_settles_both_in_position_order() =>
            SettledPositions(result).ShouldBe([1, 2]);

        [Fact]
        public void it_moves_the_money_along() =>
            BalanceOf(result, "3333333333333333").ShouldBe(100.00m);
    }

    public class when_two_transfers_compete_for_the_same_funds
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 100.00m), AnAccount("2222222222222222", 0.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "1111111111111111", "3333333333333333", 100.00m)
            ]);

        [Fact]
        public void it_settles_the_earlier_one() =>
            At(result, 1).Status.ShouldBe(TransferStatus.Settled);

        [Fact]
        public void it_rejects_the_later_one_for_insufficient_funds() =>
            At(result, 2).Reason.ShouldBe(RejectionReason.InsufficientFunds);

        [Fact]
        public void it_moves_only_the_earlier_amount()
        {
            BalanceOf(result, "2222222222222222").ShouldBe(100.00m);
            BalanceOf(result, "3333333333333333").ShouldBe(0.00m);
        }
    }

    public class when_a_transfer_breaks_a_rule
    {
        [Theory]
        [InlineData("1111111111111111", "2222222222222222", 0.00, RejectionReason.NonPositiveAmount)]
        [InlineData("1111111111111111", "2222222222222222", -5.00, RejectionReason.NonPositiveAmount)]
        [InlineData("1111111111111111", "1111111111111111", 10.00, RejectionReason.SameAccount)]
        [InlineData("9999999999999999", "2222222222222222", 10.00, RejectionReason.UnknownSendingAccount)]
        [InlineData("1111111111111111", "9999999999999999", 10.00, RejectionReason.UnknownReceivingAccount)]
        public void it_rejects_only_that_transfer(string sending, string receiving, double amount, RejectionReason reason)
        {
            var result = Settle(
                [AnAccount("1111111111111111", 500.00m), AnAccount("2222222222222222", 500.00m)],
                [
                    ATransfer(1, sending, receiving, (decimal)amount),
                    ATransfer(2, "1111111111111111", "2222222222222222", 100.00m)
                ]);

            At(result, 1).Reason.ShouldBe(reason);
            At(result, 2).Status.ShouldBe(TransferStatus.Settled);
            BalanceOf(result, "1111111111111111").ShouldBe(400.00m);
            BalanceOf(result, "2222222222222222").ShouldBe(600.00m);
        }
    }

    public class when_a_transfer_to_an_unknown_account_would_be_funded_later
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 100.00m)],
            [
                ATransfer(1, "1111111111111111", "9999999999999999", 50.00m),
                ATransfer(2, "2222222222222222", "1111111111111111", 100.00m)
            ]);

        [Fact]
        public void it_rejects_it_for_the_unknown_account() =>
            At(result, 1).Reason.ShouldBe(RejectionReason.UnknownReceivingAccount);

        [Fact]
        public void it_does_not_retry_it() =>
            BalanceOf(result, "1111111111111111").ShouldBe(100.00m);
    }

    public class when_several_rejection_reasons_apply
    {
        [Theory]
        [InlineData("9999999999999999", "9999999999999999", -5.00, RejectionReason.NonPositiveAmount)]
        [InlineData("9999999999999999", "9999999999999999", 10.00, RejectionReason.SameAccount)]
        [InlineData("9999999999999999", "8888888888888888", 10.00, RejectionReason.UnknownSendingAccount)]
        [InlineData("1111111111111111", "8888888888888888", 10.00, RejectionReason.UnknownReceivingAccount)]
        public void it_rejects_with_the_first_that_applies(string sending, string receiving, double amount, RejectionReason reason)
        {
            var result = Settle(
                [AnAccount("1111111111111111", 0.00m)],
                [ATransfer(1, sending, receiving, (decimal)amount)]);

            At(result, 1).Reason.ShouldBe(reason);
        }
    }

    public class when_the_sending_account_is_short_but_receives_funds_later
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 100.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "3333333333333333", 100.00m),
                ATransfer(2, "2222222222222222", "1111111111111111", 100.00m)
            ]);

        [Fact]
        public void it_settles_it_on_a_later_pass() =>
            SettledPositions(result).ShouldBe([2, 1]);

        [Fact]
        public void it_moves_the_money_on()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(0.00m);
            BalanceOf(result, "3333333333333333").ShouldBe(100.00m);
        }
    }

    public class when_a_later_transfer_overtakes_an_earlier_unsettled_one
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 50.00m), AnAccount("2222222222222222", 0.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "1111111111111111", "3333333333333333", 50.00m)
            ]);

        [Fact]
        public void it_settles_the_later_one() =>
            At(result, 2).Status.ShouldBe(TransferStatus.Settled);

        [Fact]
        public void it_rejects_the_earlier_one_for_insufficient_funds() =>
            At(result, 1).Reason.ShouldBe(RejectionReason.InsufficientFunds);

        [Fact]
        public void it_empties_the_sending_account() =>
            BalanceOf(result, "1111111111111111").ShouldBe(0.00m);
    }

    public class when_unsettled_transfers_are_retried
    {
        readonly SettlementResult result = Settle(
            [
                AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 0.00m),
                AnAccount("3333333333333333", 100.00m), AnAccount("4444444444444444", 0.00m)
            ],
            [
                ATransfer(1, "1111111111111111", "4444444444444444", 100.00m),
                ATransfer(2, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(3, "3333333333333333", "1111111111111111", 100.00m)
            ]);

        [Fact]
        public void it_retries_them_in_position_order() =>
            SettledPositions(result).ShouldBe([3, 1]);

        [Fact]
        public void it_rejects_the_one_left_short() =>
            At(result, 2).Reason.ShouldBe(RejectionReason.InsufficientFunds);
    }

    public class when_a_shortfall_has_knock_on_effects
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 0.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "2222222222222222", "3333333333333333", 100.00m)
            ]);

        [Fact]
        public void it_rejects_both_for_insufficient_funds()
        {
            At(result, 1).Reason.ShouldBe(RejectionReason.InsufficientFunds);
            At(result, 2).Reason.ShouldBe(RejectionReason.InsufficientFunds);
        }

        [Fact]
        public void it_leaves_every_balance_unchanged()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(0.00m);
            BalanceOf(result, "2222222222222222").ShouldBe(0.00m);
            BalanceOf(result, "3333333333333333").ShouldBe(0.00m);
        }
    }

    public class when_unfunded_accounts_pay_each_other_in_a_cycle
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "2222222222222222", "1111111111111111", 100.00m)
            ]);

        [Fact]
        public void it_rejects_both_for_insufficient_funds()
        {
            At(result, 1).Reason.ShouldBe(RejectionReason.InsufficientFunds);
            At(result, 2).Reason.ShouldBe(RejectionReason.InsufficientFunds);
        }

        [Fact]
        public void it_leaves_every_balance_unchanged()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(0.00m);
            BalanceOf(result, "2222222222222222").ShouldBe(0.00m);
        }
    }

    public class when_a_pass_settles_something
    {
        readonly SettlementResult result = Settle(
            [
                AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 0.00m),
                AnAccount("3333333333333333", 0.00m), AnAccount("4444444444444444", 100.00m)
            ],
            [
                ATransfer(1, "1111111111111111", "2222222222222222", 100.00m),
                ATransfer(2, "2222222222222222", "3333333333333333", 100.00m),
                ATransfer(3, "3333333333333333", "1111111111111111", 50.00m),
                ATransfer(4, "4444444444444444", "1111111111111111", 100.00m)
            ]);

        [Fact]
        public void it_runs_another_pass() =>
            SettledPositions(result).ShouldBe([4, 1, 2, 3]);

        [Fact]
        public void it_moves_the_money_round()
        {
            BalanceOf(result, "1111111111111111").ShouldBe(50.00m);
            BalanceOf(result, "3333333333333333").ShouldBe(50.00m);
        }
    }

    public class when_each_transfer_waits_on_the_one_after_it
    {
        // Account 0 is funded. Transfer n moves money from account n to account n + 1, but the
        // Transfers are listed last first, so each Pass can settle only one of them.
        const int Count = 50;

        [Fact]
        public void it_keeps_passing_until_all_are_settled()
        {
            var accounts = new List<Account> { AnAccount(0.ToString("D16"), 100.00m) };
            var transfers = new List<Transfer>();
            for (var n = 0; n < Count; n++)
            {
                accounts.Add(AnAccount((n + 1).ToString("D16"), 0.00m));
                transfers.Add(ATransfer(Count - n, n.ToString("D16"), (n + 1).ToString("D16"), 100.00m));
            }

            var result = Settle(accounts.ToArray(), transfers.ToArray());

            result.Settled.Count.ShouldBe(Count);
            BalanceOf(result, Count.ToString("D16")).ShouldBe(100.00m);
        }
    }

    public class when_the_outcomes_are_reported
    {
        readonly SettlementResult result = Settle(
            [AnAccount("1111111111111111", 0.00m), AnAccount("2222222222222222", 100.00m), AnAccount("3333333333333333", 0.00m)],
            [
                ATransfer(1, "1111111111111111", "3333333333333333", 100.00m),
                ATransfer(2, "3333333333333333", "1111111111111111", 500.00m),
                ATransfer(3, "2222222222222222", "1111111111111111", 100.00m),
                ATransfer(4, "2222222222222222", "2222222222222222", -7.25m)
            ]);

        [Fact]
        public void it_lists_the_settled_transfers_in_the_order_they_settled() =>
            SettledPositions(result).ShouldBe([3, 1]);

        [Fact]
        public void it_lists_the_rejected_transfers_in_position_order() =>
            result.Rejected.Select(transfer => transfer.Position).ShouldBe([2, 4]);

        [Fact]
        public void it_gives_each_rejected_transfer_its_reason_and_requested_amount()
        {
            result.Rejected[0].Reason.ShouldBe(RejectionReason.InsufficientFunds);
            result.Rejected[1].Reason.ShouldBe(RejectionReason.NonPositiveAmount);
            result.Rejected[1].Requested.ShouldBe(-7.25m);
        }

        [Fact]
        public void it_gives_the_closing_accounts() =>
            result.Accounts.All.Select(account => account.Number.Value)
                .ShouldBe(["1111111111111111", "2222222222222222", "3333333333333333"]);
    }
}
