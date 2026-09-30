namespace Bank.Domain.Specs;

public class MoneySpec
{
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
}
