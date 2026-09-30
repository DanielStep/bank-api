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

## Known limitations

- Uploading the same file again settles it again (no idempotency).
- One Company only.
- One Settlement runs at a time.
- A `GET /accounts` that arrives while a Settlement is writing the balances file can fail.
- Outcomes depend on the order of the Transfers in the file (see [ADR 0001](docs/adr/0001-gross-multi-pass-settlement.md)).

## Design

The agreed design is in [docs/scoping/design-summary.md](docs/scoping/design-summary.md), the domain language in [docs/CONTEXT.md](docs/CONTEXT.md), and the architecture decisions in [docs/adr](docs/adr).
