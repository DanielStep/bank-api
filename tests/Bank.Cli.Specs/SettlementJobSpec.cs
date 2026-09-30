namespace Bank.Cli.Specs;

public class SettlementJobSpec
{
    public class when_a_line_is_malformed : IDisposable
    {
        readonly JobRun run = new(Samples.Balances,
            "1111234522226789,1212343433335665,500.00\n" +
            "1111234522226789,1212343433335665\n" +
            "1111234522226789,1212343433335665,500.00\n");

        public void Dispose() => run.Dispose();

        [Fact]
        public void it_exits_with_1() =>
            run.ExitCode.ShouldBe(1);

        [Fact]
        public void it_prints_the_error_with_its_line_number() =>
            run.Error.ShouldContain("line 2: Expected 3 fields but found 2.");

        [Fact]
        public void it_changes_no_balance() =>
            File.ReadAllText(run.BalancesPath).ShouldBe(Samples.Balances);
    }

    public class when_the_balances_file_is_malformed : IDisposable
    {
        readonly JobRun run = new("1111111111111111,ten\n", Samples.Transfers);

        public void Dispose() => run.Dispose();

        [Fact]
        public void it_exits_with_1() =>
            run.ExitCode.ShouldBe(1);

        [Fact]
        public void it_prints_an_error_that_names_the_file() =>
            run.Error.ShouldContain(run.BalancesPath);
    }

    public class when_the_batch_is_mixed : IDisposable
    {
        const string ClosingBalances =
            "1111234522226789,4900.00\n" +
            "1111234522221234,10000.00\n" +
            "2222123433331212,50.00\n" +
            "1212343433335665,1800.00\n" +
            "3212343433335755,50000.00\n";

        readonly JobRun run = new(Samples.Balances,
            "2222123433331212,1212343433335665,600.00\n" +
            "1111234522226789,1111234522226789,10.00\n" +
            "1111234522226789,2222123433331212,100.00\n");

        public void Dispose() => run.Dispose();

        [Fact]
        public void it_exits_with_0() =>
            run.ExitCode.ShouldBe(0);

        [Fact]
        public void it_reports_the_settled_transfers_in_settle_order() =>
            Json.Compact(run.Report.GetProperty("settled")).ShouldBe(
                "[{\"line\":3,\"from\":\"1111234522226789\",\"to\":\"2222123433331212\",\"amount\":100.00}," +
                "{\"line\":1,\"from\":\"2222123433331212\",\"to\":\"1212343433335665\",\"amount\":600.00}]");

        [Fact]
        public void it_reports_the_rejected_transfers_with_their_reasons() =>
            Json.Compact(run.Report.GetProperty("rejected")).ShouldBe(
                "[{\"line\":2,\"from\":\"1111234522226789\",\"to\":\"1111234522226789\",\"amount\":10.00,\"reason\":\"SameAccount\"}]");

        [Fact]
        public void it_reports_the_closing_balance_of_every_account() =>
            Json.Compact(run.Report.GetProperty("balances")).ShouldBe(
                "[{\"accountNumber\":\"1111234522226789\",\"balance\":4900.00}," +
                "{\"accountNumber\":\"1111234522221234\",\"balance\":10000.00}," +
                "{\"accountNumber\":\"2222123433331212\",\"balance\":50.00}," +
                "{\"accountNumber\":\"1212343433335665\",\"balance\":1800.00}," +
                "{\"accountNumber\":\"3212343433335755\",\"balance\":50000.00}]");

        [Fact]
        public void it_writes_the_closing_balances_to_the_balances_file() =>
            File.ReadAllText(run.BalancesPath).ShouldBe(ClosingBalances);

        [Fact]
        public void it_prints_the_updated_balances_file() =>
            run.Output.ShouldEndWith($"Balances in {run.BalancesPath}:{Environment.NewLine}{ClosingBalances}");
    }
}
