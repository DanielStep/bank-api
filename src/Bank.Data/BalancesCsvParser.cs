using System.Globalization;
using Bank.Domain;

namespace Bank.Data;

public static class BalancesCsvParser
{
    public static Accounts Parse(string csv)
    {
        var lines = SplitLines(csv);

        var accounts = new List<Account>();
        for (var i = 0; i < lines.Count; i++)
            accounts.Add(ParseAccount(lines[i], i + 1));

        try
        {
            return new Accounts(accounts);
        }
        catch (ArgumentException e)
        {
            throw new InvalidDataException(e.Message);
        }
    }

    private static Account ParseAccount(string line, int lineNumber)
    {
        var fields = line.Split(',');
        if (fields.Length != 2)
            throw BadLine(lineNumber, $"expected 2 fields but found {fields.Length}.");

        if (!AccountNumber.IsValid(fields[0]))
            throw BadLine(lineNumber, $"'{fields[0]}' is not a 16-digit account number.");

        if (!decimal.TryParse(fields[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var balance))
            throw BadLine(lineNumber, $"'{fields[1]}' is not a number.");

        if (!Money.IsValid(balance))
            throw BadLine(lineNumber, $"'{fields[1]}' is below 0.00 or has more than 2 decimal places.");

        return new Account(new AccountNumber(fields[0]), new Money(balance));
    }

    private static InvalidDataException BadLine(int lineNumber, string message)
    {
        return new InvalidDataException($"line {lineNumber}: {message}");
    }

    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        foreach (var line in text.Split('\n'))
            lines.Add(line.EndsWith('\r') ? line.Substring(0, line.Length - 1) : line);

        if (lines[lines.Count - 1] == "")
            lines.RemoveAt(lines.Count - 1);

        return lines;
    }
}
