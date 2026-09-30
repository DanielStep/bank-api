using System.Text.Json;

namespace Bank.Api.Specs;

public class GetAccountsSpec
{
    static decimal BalanceOf(string json, string accountNumber)
    {
        foreach (var account in JsonDocument.Parse(json).RootElement.EnumerateArray())
        {
            if (account.GetProperty("accountNumber").GetString() == accountNumber)
                return account.GetProperty("balance").GetDecimal();
        }

        throw new InvalidOperationException($"No balance for {accountNumber}.");
    }

    public class when_the_service_has_just_started
    {
        [Fact]
        public async Task it_lists_the_opening_balances_in_file_order()
        {
            await using var api = new BankApiFactory(Samples.Balances);

            var json = await api.GetAccounts();

            json.ShouldBe(
                "[{\"accountNumber\":\"1111234522226789\",\"balance\":5000.00}," +
                "{\"accountNumber\":\"1111234522221234\",\"balance\":10000.00}," +
                "{\"accountNumber\":\"2222123433331212\",\"balance\":550.00}," +
                "{\"accountNumber\":\"1212343433335665\",\"balance\":1200.00}," +
                "{\"accountNumber\":\"3212343433335755\",\"balance\":50000.00}]");
        }
    }

    public class when_a_transfer_batch_has_been_uploaded : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        string json = "";

        public async ValueTask InitializeAsync()
        {
            await api.PostTransferBatch(Samples.Transfers);
            json = await api.GetAccounts();
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_lists_the_balances_after_the_settlement()
        {
            BalanceOf(json, "1111234522226789").ShouldBe(4820.50m);
            BalanceOf(json, "3212343433335755").ShouldBe(48679.50m);
        }
    }

    public class when_the_balances_file_is_malformed
    {
        [Fact]
        public async Task it_refuses_to_start_with_an_error_that_names_the_file()
        {
            await using var api = new BankApiFactory("1111111111111111,ten\n");

            Should.Throw<InvalidDataException>(() => api.CreateClient())
                .Message.ShouldContain(api.BalancesPath);
        }
    }

    public class when_the_balances_file_lists_an_account_twice
    {
        [Fact]
        public async Task it_refuses_to_start_with_an_error_that_names_the_account()
        {
            await using var api = new BankApiFactory("1111111111111111,10.00\n1111111111111111,20.00\n");

            Should.Throw<InvalidDataException>(() => api.CreateClient())
                .Message.ShouldContain("1111111111111111");
        }
    }
}
