namespace Bank.Application;

public class ParseResult
{
    public IReadOnlyList<TransferRow> Rows { get; }
    public IReadOnlyList<CsvError> Errors { get; }

    public ParseResult(IReadOnlyList<TransferRow> rows, IReadOnlyList<CsvError> errors)
    {
        Rows = rows;
        Errors = errors;
    }
}
