namespace Bank.Domain.Specs;

public class MoneySpec
{
    public class when_given_zero_or_2_decimal_places
    {
        [Fact]
        public void it_accepts_zero() =>
            new Money(0.00m).Value.ShouldBe(0.00m);

        [Fact]
        public void it_accepts_2_decimal_places() =>
            new Money(120.50m).Value.ShouldBe(120.50m);
    }

    public class when_given_a_negative_amount
    {
        [Fact]
        public void it_is_refused() =>
            Should.Throw<ArgumentException>(() => new Money(-0.01m));
    }

    public class when_given_more_than_2_decimal_places
    {
        [Fact]
        public void it_is_refused() =>
            Should.Throw<ArgumentException>(() => new Money(10.001m));

        [Fact]
        public void it_is_refused_even_when_the_extra_places_are_zero() =>
            Should.Throw<ArgumentException>(() => new Money(5.100m));
    }

    public class when_adding_and_subtracting
    {
        [Fact]
        public void it_adds() =>
            new Money(100.00m).Add(new Money(0.50m)).ShouldBe(new Money(100.50m));

        [Fact]
        public void it_subtracts() =>
            new Money(100.00m).Subtract(new Money(0.50m)).ShouldBe(new Money(99.50m));
    }
}
