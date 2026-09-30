namespace Bank.Application.Specs;

public class TransferCsvParserSpec
{
    public class when_the_file_is_well_formed
    {
        readonly ParseResult result = TransferCsvParser.Parse(Samples.Transfers);

        [Fact]
        public void it_gives_one_row_per_line_numbered_from_1() =>
            result.Rows.Select(row => row.Line).ShouldBe([1, 2, 3, 4]);

        [Fact]
        public void it_reads_the_fields_in_order()
        {
            result.Rows[0].From.ShouldBe("1111234522226789");
            result.Rows[0].To.ShouldBe("1212343433335665");
            result.Rows[0].Amount.ShouldBe(500.00m);
        }

        [Fact]
        public void it_reports_no_errors() =>
            result.Errors.ShouldBeEmpty();

        [Theory]
        [InlineData("\n", true)]
        [InlineData("\n", false)]
        [InlineData("\r\n", true)]
        [InlineData("\r\n", false)]
        public void it_accepts_either_line_ending(string ending, bool endsWithOne)
        {
            var csv = Samples.Transfers.TrimEnd('\n').Replace("\n", ending);
            if (endsWithOne)
                csv += ending;

            var parsed = TransferCsvParser.Parse(csv);

            parsed.Errors.ShouldBeEmpty();
            parsed.Rows.Select(row => row.To).ShouldBe(
                ["1212343433335665", "2222123433331212", "1111234522226789", "1212343433335665"]);
        }

        [Theory]
        [InlineData("-5.00", -5.00)]
        [InlineData("+5", 5)]
        public void it_accepts_a_signed_amount(string amount, double expected) =>
            TransferCsvParser.Parse($"1111234522226789,1212343433335665,{amount}").Rows[0].Amount.ShouldBe((decimal)expected);
    }

    public class when_a_line_is_malformed
    {
        [Theory]
        [InlineData("1111234522226789,1212343433335665")]
        [InlineData("1111234522226789,1212343433335665,500.00,extra")]
        [InlineData("111123452222678,1212343433335665,500.00")]
        [InlineData("1111234522226789,12123434333356650,500.00")]
        [InlineData("1111234522226789,121234343333566X,500.00")]
        [InlineData("1111234522226789,1212343433335665,five")]
        [InlineData("1111234522226789,1212343433335665,500.005")]
        [InlineData("1111234522226789,1212343433335665,")]
        [InlineData("")]
        public void it_reports_an_error_for_that_line_only(string line)
        {
            var result = TransferCsvParser.Parse(
                "1111234522226789,1212343433335665,500.00\n" + line + "\n1111234522226789,1212343433335665,500.00\n");

            result.Errors.Select(error => error.Line).ShouldBe([2]);
            result.Rows.ShouldBeEmpty();
        }
    }

    public class when_several_lines_are_malformed
    {
        [Fact]
        public void it_reports_every_one()
        {
            var result = TransferCsvParser.Parse(
                "1111234522226789,1212343433335665,abc\n1111234522226789,1212343433335665,500.00\n1111234522226789,1212343433335665\n");

            result.Errors.Select(error => error.Line).ShouldBe([1, 3]);
        }
    }

    public class when_the_file_holds_no_transfers
    {
        [Theory]
        [InlineData("")]
        [InlineData("\n")]
        public void it_reports_one_error_on_line_1(string csv) =>
            TransferCsvParser.Parse(csv).Errors.Select(error => error.Line).ShouldBe([1]);
    }
}
