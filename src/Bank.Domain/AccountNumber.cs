namespace Bank.Domain;

public readonly record struct AccountNumber
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"'{value}' is not a 16-digit account number.");
        Value = value;
    }

    public static bool IsValid(string value) => value.Length == 16 && value.All(char.IsAsciiDigit);

    public override string ToString() => Value;
}
