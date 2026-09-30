using Bank.Domain;

namespace Bank.Application;

public class SettleTransferBatchHandler
{
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

        var result = batch.Settle(repository.GetAll());
        repository.SaveAll(result.Accounts);
        return new SettleTransferBatchOutcome(result, new List<CsvError>());
    }
}
