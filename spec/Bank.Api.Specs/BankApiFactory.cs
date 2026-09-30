using System.Text;
using Bank.Data;
using Bank.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Api.Specs;

// Runs the Api in-process on its own temporary balances file, deleted when the spec is done with it.
class BankApiFactory : WebApplicationFactory<Program>
{
    public string BalancesPath { get; }

    public BankApiFactory(string balances)
    {
        BalancesPath = Path.GetTempFileName();
        File.WriteAllText(BalancesPath, balances);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            services.AddSingleton<IAccountRepository>(new FileAccountRepository(BalancesPath)));
    }

    public Task<HttpResponseMessage> PostTransferBatch(string csv)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "transfers.csv");

        return CreateClient().PostAsync("/transfer-batches", form);
    }

    public Task<string> GetAccounts()
    {
        return CreateClient().GetStringAsync("/accounts");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(BalancesPath);
    }
}
