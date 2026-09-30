using Bank.Domain;

namespace Bank.Data.Specs;

public class BalancesCsvParserSpec
{
    public class when_the_file_is_well_formed
    {
        readonly Accounts accounts = BalancesCsvParser.Parse(Samples.Balances);

        [Fact]
        public void it_gives_every_account_in_file_order() =>
            accounts.All.Select(account => account.Number.Value).ShouldBe(
                ["1111234522226789", "1111234522221234", "2222123433331212", "1212343433335665", "3212343433335755"]);

        [Fact]
        public void it_reads_each_balance() =>
            accounts.All.Select(account => account.Balance.Value).ShouldBe([5000.00m, 10000.00m, 550.00m, 1200.00m, 50000.00m]);

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void it_accepts_either_line_ending(string ending) =>
            BalancesCsvParser.Parse(Samples.Balances.Replace("\n", ending)).All.Count.ShouldBe(5);
    }

    public class when_a_line_is_malformed
    {
        [Theory]
        [InlineData("1111111111111111")]
        [InlineData("1111111111111111,10.00,extra")]
        [InlineData("111111111111111,10.00")]
        [InlineData("1111111111111111,ten")]
        [InlineData("1111111111111111,-0.01")]
        [InlineData("1111111111111111,10.001")]
        public void it_throws_an_error_that_names_the_line(string line) =>
            Should.Throw<InvalidDataException>(() => BalancesCsvParser.Parse("2222222222222222,10.00\n" + line + "\n"))
                .Message.ShouldContain("line 2");
    }

    public class when_an_account_is_listed_twice
    {
        [Fact]
        public void it_throws_an_error_that_names_the_account() =>
            Should.Throw<InvalidDataException>(() => BalancesCsvParser.Parse("1111111111111111,10.00\n1111111111111111,20.00\n"))
                .Message.ShouldContain("1111111111111111");
    }
}
