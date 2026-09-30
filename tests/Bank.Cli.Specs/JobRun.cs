using System.Text.Json;

namespace Bank.Cli.Specs;

class JobRun : IDisposable
{
    public string BalancesPath { get; } = Path.GetTempFileName();
    public string TransfersPath { get; } = Path.GetTempFileName();
    public int ExitCode { get; }
    public string Output { get; }
    public string Error { get; }

    public JobRun(string balances, string transfers)
    {
        File.WriteAllText(BalancesPath, balances);
        File.WriteAllText(TransfersPath, transfers);

        var output = new StringWriter();
        var error = new StringWriter();
        ExitCode = new SettlementJob(output, error).Run([BalancesPath, TransfersPath]);
        Output = output.ToString();
        Error = error.ToString();
    }

    public JsonElement Report => JsonDocument.Parse(Output.Substring(0, Output.LastIndexOf('}') + 1)).RootElement;

    public void Dispose()
    {
        File.Delete(BalancesPath);
        File.Delete(TransfersPath);
    }
}
