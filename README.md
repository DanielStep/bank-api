# Bank Settlement

A console batch job for one Company. It loads the Company's Account Balances from one CSV file, settles the day's Transfer Batch from another, and writes the closing Balances back, so that no Account ever goes below $0.

Built with .NET 10

## Install .NET 10

- **Windows:** `winget install Microsoft.DotNet.SDK.10`
- **Mac:** run the SDK installer from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).

## Run it

From the repo root, pass the balances file and then the Transfer Batch file:

```sh
dotnet run --project src/Bank.Cli -- mable_account_balances.csv mable_transactions.csv
```

There is nothing to configure. The job prints a JSON report of the Settled Transfers, the Rejected Transfers with their Rejection Reasons, and the closing Balances. It then writes the closing Balances to the balances file and prints that file. It exits with 0.

A malformed Transfer Batch file prints line-numbered errors, exits with 1 and changes no Balance. So does a malformed balances file, a missing file or the wrong number of arguments.

## Resetting the Balances

The job overwrites the balances file it is given, so running it on the sample changes `mable_account_balances.csv` in the repo, and running it again settles from the new Balances. The specs never read or write that file. To go back to the opening Balances:

```sh
git checkout mable_account_balances.csv
```

## Run the specs

```sh
dotnet test
```

## Decisions & Assumptions

- Transfers are settled gross (one at a time and only if the sender holds the full amount at that moment) because
  net settlement (summing all sent and received for account at once) turns into an complex and potentially unfair subset-selection problem of which transactions to reject once any Account nets negative (see [ADR 0001](docs/adr/0001-gross-multi-pass-settlement.md)); simpler gross method chosen due to time constraints.
- Console batch job, because the brief asks for a system that loads the Balances and then accepts a day's Transfers. The two files are passed as arguments.
- Persistance and provision of account balance in csv file for simplicity. Repository pattern used to easily swap in SQLite database.
- Simplified CQRS pattern used to encapulate application logic with single responsbility; no need for a mediator pattern yet.

## Known limitations

- Running the same Transfer Batch again settles it again (no idempotency). Can be achieved by persisting batch status but deemed beyond scope of exercise.
- One Company only.
- Gross settlement in order means that:
  - Outcomes depend on the order of the Transfers in the file.
  - Rejects a cycle of Transfers that no sender can cover on its own, even when netting would settle it and move money. (see `when_underfunded_accounts_pay_each_other_unequal_amounts_in_a_cycle` in [TransferBatchSpec.cs](tests/Bank.Domain.Specs/TransferBatchSpec.cs)).

## Design

The design and scoping is in [docs/scoping/design-summary.md](docs/scoping/design-summary.md), the domain language in [docs/CONTEXT.md](docs/CONTEXT.md), and the architecture decisions in [docs/adr](docs/adr).
