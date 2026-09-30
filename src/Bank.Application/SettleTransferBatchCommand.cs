namespace Bank.Application;

public class SettleTransferBatchCommand
{
    public string Csv { get; }

    public SettleTransferBatchCommand(string csv)
    {
        Csv = csv;
    }
}
