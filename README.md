# Bank API

A banking service for one Company. It holds the Company's Account Balances and settles each day's Transfer Batch, uploaded as a CSV file, so that no Account ever goes below $0.

Built with .NET 10

## Install .NET 10

- **Windows:** `winget install Microsoft.DotNet.SDK.10`
- **Mac:** run the SDK installer from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).

## Run it

From the repo root:

```sh
dotnet run --project src/Bank.Api
```

The service listens on `http://localhost:5080`, with nothing to configure. In a second terminal, from the repo root, upload the sample Transfer Batch and then read the Balances:

```sh
curl -F file=@mable_transactions.csv http://localhost:5080/transfer-batches
curl http://localhost:5080/accounts
```

The upload returns the Settled Transfers, the Rejected Transfers with their Rejection Reasons, and the closing Balances. A malformed CSV returns 400 with line-numbered errors, and no Balance changes.

## Run the specs

```sh
dotnet test
```

## Resetting the Balances

The service works on a copy of `mable_account_balances.csv` in its build output, so the file in the repo is never modified. Balances are kept across restarts. To go back to the opening Balances:

```sh
dotnet clean
```

The copy is made with `PreserveNewest`, so editing the repo's `mable_account_balances.csv` also replaces the working copy on the next build, and the Settled Balances are lost.

## Decisions & Assumptions

- Transfers are settled gross (one at a time and only if the sender holds the full amount at that moment) because
  net settlement turns into an unfair subset-selection problem once any Account nets negative (see [ADR 0001](docs/adr/0001-gross-multi-pass-settlement.md))
- Minimal API used on the basis that company is specified to provide the file in some way. Console batch application would be marginally simpler but less usable.
- Persistance and provision of account balance in csv file for simplicity. Repository pattern used to easily swap in SQLite database.
- Simplified CQRS pattern used to encapulate application logic with single responsbility; no need for a mediator pattern yet.

## Known limitations

- Uploading the same file again settles it again (no idempotency). Can be achieved by persisting batch status but deemed beyond scope of exercise.
- One Company only.
- One Settlement runs at a time.
- Outcomes depend on the order of the Transfers in the file.

## Design

The design and scoping is in [docs/scoping/design-summary.md](docs/scoping/design-summary.md), the domain language in [docs/CONTEXT.md](docs/CONTEXT.md), and the architecture decisions in [docs/adr](docs/adr).
