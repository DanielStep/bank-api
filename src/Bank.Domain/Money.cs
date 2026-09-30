namespace Bank.Domain;

public readonly record struct Money
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"{value} is not a non-negative amount with at most 2 decimal places.");
        Value = value;
    }

    public static bool IsValid(decimal value) => value >= 0 && value.Scale <= 2;

    public Money Add(Money other) => new(Value + other.Value);

    public Money Subtract(Money other) => new(Value - other.Value);

    public bool IsLessThan(Money other) => Value < other.Value;
}
