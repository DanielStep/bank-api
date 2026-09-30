namespace Bank.Application;

public class CsvError
{
    public int Line { get; }
    public string Message { get; }

    public CsvError(int line, string message)
    {
        Line = line;
        Message = message;
    }
}
