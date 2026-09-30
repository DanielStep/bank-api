namespace Bank.Cli.Specs;

public class SampleFileIntegrationSpec : IDisposable
{
    readonly JobRun run = new(Samples.Balances, RepoFile("mable_transactions.csv"));

    static string RepoFile(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "BankBatchJob.slnx")))
            directory = directory.Parent ?? throw new InvalidOperationException("No BankBatchJob.slnx above the spec's output.");

        return File.ReadAllText(Path.Combine(directory.FullName, name));
    }

    public void Dispose() => run.Dispose();

    [Fact]
    public void it_exits_with_0() =>
        run.ExitCode.ShouldBe(0);

    [Fact]
    public void it_settles_all_four_lines_in_order() =>
        Json.LinesOf(run.Report.GetProperty("settled")).ShouldBe([1, 2, 3, 4]);

    [Fact]
    public void it_rejects_none() =>
        run.Report.GetProperty("rejected").GetArrayLength().ShouldBe(0);

    [Fact]
    public void it_gives_the_expected_closing_balances() =>
        File.ReadAllText(run.BalancesPath).ShouldBe(
            "1111234522226789,4820.50\n" +
            "1111234522221234,9974.40\n" +
            "2222123433331212,1550.00\n" +
            "1212343433335665,1725.60\n" +
            "3212343433335755,48679.50\n");
}
