using System.Globalization;
using Bank.Domain;

namespace Bank.Application;

public static class TransferCsvParser
{
    public static ParseResult Parse(string csv)
    {
        var lines = SplitLines(csv);
        if (lines.Count == 0 || (lines.Count == 1 && lines[0] == ""))
            return new ParseResult(new List<TransferRow>(), new List<CsvError> { new CsvError(1, "The file holds no Transfers.") });

        var rows = new List<TransferRow>();
        var errors = new List<CsvError>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = i + 1;
            var fields = lines[i].Split(',');
            var error = FindError(fields, out var amount);

            if (error != null)
                errors.Add(new CsvError(line, error));
            else
                rows.Add(new TransferRow(line, fields[0], fields[1], amount));
        }

        if (errors.Count > 0)
            return new ParseResult(new List<TransferRow>(), errors);

        return new ParseResult(rows, errors);
    }

    private static string? FindError(string[] fields, out decimal amount)
    {
        amount = 0;

        if (fields.Length != 3)
            return $"Expected 3 fields but found {fields.Length}.";

        if (!AccountNumber.IsValid(fields[0]))
            return $"'{fields[0]}' is not a 16-digit account number.";

        if (!AccountNumber.IsValid(fields[1]))
            return $"'{fields[1]}' is not a 16-digit account number.";

        if (!TryParseAmount(fields[2], out amount))
            return $"'{fields[2]}' is not a number.";

        if (amount.Scale > 2)
            return $"'{fields[2]}' has more than 2 decimal places.";

        return null;
    }

    private static bool TryParseAmount(string text, out decimal amount) =>
        decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);

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
