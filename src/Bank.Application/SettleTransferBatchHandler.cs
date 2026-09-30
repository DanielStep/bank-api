using Bank.Domain;

namespace Bank.Application;

public class SettleTransferBatchHandler
{
    // Only one Settlement runs at a time, so none of its changes is lost.
    private static readonly object SettleLock = new();

    private readonly IAccountRepository repository;

    public SettleTransferBatchHandler(IAccountRepository repository)
    {
        this.repository = repository;
    }

    public SettleTransferBatchOutcome Handle(SettleTransferBatchCommand command)
    {
        var parsed = TransferCsvParser.Parse(command.Csv);
        if (parsed.Errors.Count > 0)
            return new SettleTransferBatchOutcome(null, parsed.Errors);

        var transfers = new List<Transfer>();
        foreach (var row in parsed.Rows)
        {
            var sending = new AccountNumber(row.From);
            var receiving = new AccountNumber(row.To);
            transfers.Add(new Transfer(row.Line, sending, receiving, row.Amount));
        }

        var batch = new TransferBatch(transfers);

        lock (SettleLock)
        {
            var result = batch.Settle(repository.GetAll());
            repository.SaveAll(result.Accounts);
            return new SettleTransferBatchOutcome(result, new List<CsvError>());
        }
    }
}
