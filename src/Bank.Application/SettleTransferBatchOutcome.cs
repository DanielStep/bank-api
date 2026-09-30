using Bank.Domain;

namespace Bank.Application;

public class SettleTransferBatchOutcome
{
    public SettlementResult? Result { get; }
    public IReadOnlyList<CsvError> Errors { get; }

    public SettleTransferBatchOutcome(SettlementResult? result, IReadOnlyList<CsvError> errors)
    {
        Result = result;
        Errors = errors;
    }
}
