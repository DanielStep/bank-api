namespace Bank.Specs;

// The contents of mable_account_balances.csv and mable_transactions.csv.
static class Samples
{
    public const string Balances =
        "1111234522226789,5000.00\n" +
        "1111234522221234,10000.00\n" +
        "2222123433331212,550.00\n" +
        "1212343433335665,1200.00\n" +
        "3212343433335755,50000.00\n";

    public const string Transfers =
        "1111234522226789,1212343433335665,500.00\n" +
        "3212343433335755,2222123433331212,1000.00\n" +
        "3212343433335755,1111234522226789,320.50\n" +
        "1111234522221234,1212343433335665,25.60\n";
}
