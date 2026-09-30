using Bank.Domain;

namespace Bank.Data.Specs;

public class FileAccountRepositorySpec
{
    public class when_getting_all_accounts
    {
        [Fact]
        public void it_reads_them_from_the_file()
        {
            using var file = new TempFile("1111111111111111,10.00\n2222222222222222,20.00\n");

            var accounts = new FileAccountRepository(file.Path).GetAll();

            accounts.All.Select(account => account.Number.Value).ShouldBe(["1111111111111111", "2222222222222222"]);
        }
    }

    public class when_the_file_is_malformed
    {
        [Fact]
        public void it_throws_an_error_that_names_the_file()
        {
            using var file = new TempFile("1111111111111111,ten\n");

            Should.Throw<InvalidDataException>(() => new FileAccountRepository(file.Path).GetAll())
                .Message.ShouldContain(file.Path);
        }
    }

    public class when_saving_all_accounts : IDisposable
    {
        readonly TempFile file = new("1111111111111111,100.00\n2222222222222222,0.00\n");
        readonly FileAccountRepository repository;

        public when_saving_all_accounts()
        {
            repository = new FileAccountRepository(file.Path);

            var accounts = repository.GetAll();
            accounts.All[0].TryWithdraw(new Money(0.50m));
            accounts.All[1].Deposit(new Money(0.50m));
            repository.SaveAll(accounts);
        }

        public void Dispose() => file.Dispose();

        [Fact]
        public void it_keeps_the_balances_and_their_order() =>
            repository.GetAll().All.Select(account => account.Balance.Value).ShouldBe([99.50m, 0.50m]);

        [Fact]
        public void it_keeps_the_file_format() =>
            File.ReadAllText(file.Path).ShouldBe("1111111111111111,99.50\n2222222222222222,0.50\n");
    }
}
