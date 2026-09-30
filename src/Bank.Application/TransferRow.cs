namespace Bank.Application;

public class TransferRow
{
    public int Line { get; }
    public string From { get; }
    public string To { get; }
    public decimal Amount { get; }

    public TransferRow(int line, string from, string to, decimal amount)
    {
        Line = line;
        From = from;
        To = to;
        Amount = amount;
    }
}
