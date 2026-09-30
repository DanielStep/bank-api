using System.Globalization;
using System.Text;
using Bank.Domain;

namespace Bank.Data;

public class FileAccountRepository : IAccountRepository
{
    private readonly string path;

    public FileAccountRepository(string path)
    {
        this.path = path;
    }

    public Accounts GetAll()
    {
        try
        {
            return BalancesCsvParser.Parse(File.ReadAllText(path));
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}");
        }
    }

    public void SaveAll(Accounts accounts)
    {
        var text = new StringBuilder();
        foreach (var account in accounts.All)
            text.Append($"{account.Number},{account.Balance.Value.ToString("0.00", CultureInfo.InvariantCulture)}\n");

        File.WriteAllText(path, text.ToString());
    }
}
