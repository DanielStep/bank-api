namespace Bank.Domain.Specs;

public class AccountNumberSpec
{
    public class when_given_16_digits
    {
        [Fact]
        public void it_keeps_leading_zeros() =>
            new AccountNumber("0000123412341234").Value.ShouldBe("0000123412341234");
    }

    public class when_not_given_16_digits
    {
        [Theory]
        [InlineData("111123452222678")]
        [InlineData("11112345222267890")]
        [InlineData("111123452222678X")]
        public void it_is_refused(string value) =>
            Should.Throw<ArgumentException>(() => new AccountNumber(value));
    }
}
