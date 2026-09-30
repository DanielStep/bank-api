using System.Net;
using System.Text.Json;

namespace Bank.Api.Specs;

public class PostTransferBatchesSpec
{
    static async Task<JsonElement> JsonOf(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    static List<int> LinesOf(JsonElement list)
    {
        var lines = new List<int>();
        foreach (var item in list.EnumerateArray())
            lines.Add(item.GetProperty("line").GetInt32());

        return lines;
    }

    public class when_the_sample_batch_is_uploaded
    {
        [Fact]
        public async Task it_answers_200()
        {
            await using var api = new BankApiFactory(Samples.Balances);

            var response = await api.PostTransferBatch(Samples.Transfers);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public class when_the_request_holds_no_file : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;

        public async ValueTask InitializeAsync()
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent("nothing"), "note");
            response = await api.CreateClient().PostAsync("/transfer-batches", form);
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_400() =>
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        [Fact]
        public void it_changes_no_balance() =>
            File.ReadAllText(api.BalancesPath).ShouldBe(Samples.Balances);
    }

    public class when_the_line_endings_differ
    {
        [Theory]
        [InlineData("\n", true)]
        [InlineData("\n", false)]
        [InlineData("\r\n", true)]
        [InlineData("\r\n", false)]
        public async Task it_settles_all_four_transfers(string ending, bool endsWithLineEnding)
        {
            await using var api = new BankApiFactory(Samples.Balances);
            var csv = Samples.Transfers.TrimEnd('\n').Replace("\n", ending) + (endsWithLineEnding ? ending : "");

            var response = await api.PostTransferBatch(csv);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            LinesOf((await JsonOf(response)).GetProperty("settled")).ShouldBe([1, 2, 3, 4]);
        }
    }

    public class when_an_amount_is_negative : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;
        JsonElement json;

        public async ValueTask InitializeAsync()
        {
            response = await api.PostTransferBatch(
                "1111234522226789,1212343433335665,-5.00\n" +
                "1111234522226789,1212343433335665,500.00\n");
            json = await JsonOf(response);
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_200() =>
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

        [Fact]
        public void it_rejects_that_transfer_as_non_positive() =>
            json.GetProperty("rejected").GetRawText().ShouldBe(
                "[{\"line\":1,\"from\":\"1111234522226789\",\"to\":\"1212343433335665\",\"amount\":-5.00,\"reason\":\"NonPositiveAmount\"}]");

        [Fact]
        public void it_settles_the_rest() =>
            LinesOf(json.GetProperty("settled")).ShouldBe([2]);
    }

    public class when_a_line_is_malformed : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;
        JsonElement json;

        public async ValueTask InitializeAsync()
        {
            response = await api.PostTransferBatch(
                "1111234522226789,1212343433335665,500.00\n" +
                "1111234522226789,1212343433335665\n" +
                "1111234522226789,1212343433335665,500.00\n");
            json = await JsonOf(response);
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_400_with_problem_details()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        }

        [Fact]
        public void it_lists_the_error_with_its_line_number() =>
            LinesOf(json.GetProperty("errors")).ShouldBe([2]);

        [Fact]
        public void it_changes_no_balance() =>
            File.ReadAllText(api.BalancesPath).ShouldBe(Samples.Balances);
    }

    public class when_the_file_holds_no_transfers : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;

        public async ValueTask InitializeAsync() => response = await api.PostTransferBatch("");

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_400() =>
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        [Fact]
        public void it_changes_no_balance() =>
            File.ReadAllText(api.BalancesPath).ShouldBe(Samples.Balances);
    }

    public class when_the_batch_is_mixed : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;
        JsonElement json;

        public async ValueTask InitializeAsync()
        {
            response = await api.PostTransferBatch(
                "2222123433331212,1212343433335665,600.00\n" +
                "1111234522226789,1111234522226789,10.00\n" +
                "1111234522226789,2222123433331212,100.00\n");
            json = await JsonOf(response);
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_200() =>
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

        [Fact]
        public void it_lists_the_settled_transfers_in_settle_order() =>
            json.GetProperty("settled").GetRawText().ShouldBe(
                "[{\"line\":3,\"from\":\"1111234522226789\",\"to\":\"2222123433331212\",\"amount\":100.00}," +
                "{\"line\":1,\"from\":\"2222123433331212\",\"to\":\"1212343433335665\",\"amount\":600.00}]");

        [Fact]
        public void it_lists_the_rejected_transfers_with_their_reasons() =>
            json.GetProperty("rejected").GetRawText().ShouldBe(
                "[{\"line\":2,\"from\":\"1111234522226789\",\"to\":\"1111234522226789\",\"amount\":10.00,\"reason\":\"SameAccount\"}]");

        [Fact]
        public void it_lists_the_closing_balance_of_every_account() =>
            json.GetProperty("balances").GetRawText().ShouldBe(
                "[{\"accountNumber\":\"1111234522226789\",\"balance\":4900.00}," +
                "{\"accountNumber\":\"1111234522221234\",\"balance\":10000.00}," +
                "{\"accountNumber\":\"2222123433331212\",\"balance\":50.00}," +
                "{\"accountNumber\":\"1212343433335665\",\"balance\":1800.00}," +
                "{\"accountNumber\":\"3212343433335755\",\"balance\":50000.00}]");
    }

    public class when_every_transfer_is_rejected : IAsyncLifetime
    {
        readonly BankApiFactory api = new(Samples.Balances);
        HttpResponseMessage response = null!;
        JsonElement json;

        public async ValueTask InitializeAsync()
        {
            response = await api.PostTransferBatch("9999999999999999,1212343433335665,10.00\n");
            json = await JsonOf(response);
        }

        public ValueTask DisposeAsync() => api.DisposeAsync();

        [Fact]
        public void it_answers_200() =>
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

        [Fact]
        public void it_settles_nothing() =>
            json.GetProperty("settled").GetArrayLength().ShouldBe(0);

        [Fact]
        public void it_rejects_the_transfer_as_an_unknown_sending_account()
        {
            var rejected = json.GetProperty("rejected")[0];

            rejected.GetProperty("line").GetInt32().ShouldBe(1);
            rejected.GetProperty("reason").GetString().ShouldBe("UnknownSendingAccount");
        }

        [Fact]
        public void it_changes_no_balance() =>
            File.ReadAllText(api.BalancesPath).ShouldBe(Samples.Balances);
    }

    public class when_the_same_file_is_uploaded_twice
    {
        [Fact]
        public async Task it_settles_it_again()
        {
            await using var api = new BankApiFactory(Samples.Balances);
            await api.PostTransferBatch(Samples.Transfers);

            var response = await api.PostTransferBatch(Samples.Transfers);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            File.ReadAllText(api.BalancesPath).ShouldStartWith("1111234522226789,4641.00\n");
        }
    }

    public class when_two_uploads_arrive_together
    {
        [Fact]
        public async Task it_settles_them_one_after_another()
        {
            await using var api = new BankApiFactory(Samples.Balances);

            var responses = await Task.WhenAll(api.PostTransferBatch(Samples.Transfers), api.PostTransferBatch(Samples.Transfers));

            responses[0].StatusCode.ShouldBe(HttpStatusCode.OK);
            responses[1].StatusCode.ShouldBe(HttpStatusCode.OK);
            File.ReadAllText(api.BalancesPath).ShouldStartWith("1111234522226789,4641.00\n");
        }
    }
}
