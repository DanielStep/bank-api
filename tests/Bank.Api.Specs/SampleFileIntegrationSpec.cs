using System.Net;
using System.Text.Json;

namespace Bank.Api.Specs;

// Settles the repo's real sample files, not the copies in Samples.
public class SampleFileIntegrationSpec : IAsyncLifetime
{
    BankApiFactory api = null!;
    HttpResponseMessage response = null!;
    JsonElement json;

    static string RepoFile(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "BankApi.slnx")))
            directory = directory.Parent ?? throw new InvalidOperationException("No BankApi.slnx above the spec's output.");

        return File.ReadAllText(Path.Combine(directory.FullName, name));
    }

    public async ValueTask InitializeAsync()
    {
        api = new BankApiFactory(RepoFile("mable_account_balances.csv"));
        response = await api.PostTransferBatch(RepoFile("mable_transactions.csv"));
        json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    public ValueTask DisposeAsync() => api.DisposeAsync();

    [Fact]
    public void it_answers_200() =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    [Fact]
    public void it_settles_all_four_lines_in_order() =>
        Json.LinesOf(json.GetProperty("settled")).ShouldBe([1, 2, 3, 4]);

    [Fact]
    public void it_rejects_none() =>
        json.GetProperty("rejected").GetArrayLength().ShouldBe(0);

    [Fact]
    public void it_gives_the_expected_closing_balances() =>
        json.GetProperty("balances").GetRawText().ShouldBe(
            "[{\"accountNumber\":\"1111234522226789\",\"balance\":4820.50}," +
            "{\"accountNumber\":\"1111234522221234\",\"balance\":9974.40}," +
            "{\"accountNumber\":\"2222123433331212\",\"balance\":1550.00}," +
            "{\"accountNumber\":\"1212343433335665\",\"balance\":1725.60}," +
            "{\"accountNumber\":\"3212343433335755\",\"balance\":48679.50}]");
}
