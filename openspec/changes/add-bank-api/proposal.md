# Proposal

## Why

The Mable back end code challenge asks for a simple banking service. It holds one Company's Account balances, accepts a day's Transfer Batch as CSV, and never lets an Account go below $0. The repo holds only the agreed design ([design summary](../../../docs/design/design-summary.md)) and no code yet. This change builds the whole service in one go, as the design summary's Delivery plan requires, so the reviewer can run it with `dotnet run` and check it against the rubric and the sample files.

## What Changes

- New .NET 10 solution (`BankApi.slnx`, `global.json`) with four layers (Domain, Application, Data, Api) and one spec project per layer, as set out in the design summary's Solution layout.
- **Settlement**: gross, multi-pass Settlement of a Transfer Batch in Position order ([ADR 0001](../../../docs/adr/0001-gross-multi-pass-settlement.md)). A Transfer is Settled, left Unsettled and retried on a later Pass, or Rejected with the first Rejection Reason that applies, in the design's precedence order.
- **Transfer Batch upload**: `POST /transfer-batches` takes a 3-column CSV file with no header row. A well-formed file returns 200 with the Settled Transfers, the Rejected Transfers (with reasons) and the closing balances. A malformed file returns 400 with line-numbered errors, and nothing is applied.
- **Account balances**: `GET /accounts` returns the current balances. Balances are kept in a CSV working copy of `mable_account_balances.csv` in the build output, so the repo's file is never modified and `dotnet clean` resets the balances.
- **Hosting**: runs on Windows and Mac with only `dotnet run --project src/Bank.Api`, over plain HTTP on port 5080, with nothing to configure.
- Specs: xUnit v3 and Shouldly, in describe/context/it style ([ADR 0002](../../../docs/adr/0002-xunit-and-shouldly-in-place-of-rspec.md)), orthogonal per layer, plus a headline acceptance spec that checks the sample files' closing balances.
- README: how to run it, one `curl` line, the expected closing balances, `dotnet test`, the .NET 10 note and the known limitations.

Decisions made while proposing (the design summary leaves them open):
- The fixed port is **5080**.
- `rejected` in the 200 response lists Transfers in **Position order**.
- A CSV holding **no Transfers** (an empty file, or only a line ending) is a parse error: **400**.
- The status **Deferred is replaced by Unsettled**. A Transfer that passes its own checks starts Unsettled and stays Unsettled until it is Settled or Rejected. CONTEXT.md, the design summary, ADR 0001 and the interview log (Q34) are updated to match.

## Capabilities

### New Capabilities
- `bank-api`: the whole service, in one spec. It covers settling a Transfer Batch (the $0 floor, retrying Unsettled Transfers on later Passes, when Settlement stops, the Rejection Reasons and their precedence, and the order of the results); uploading it over HTTP (the CSV format, what counts as malformed, the 400 and 200 responses); holding the Balances (`GET /accounts`, the working balances file, and refusing to start on a bad one); and hosting (zero-config `dotnet run` over HTTP on a fixed port, one Company, one Settlement at a time).

### Modified Capabilities
None. There are no existing specs.

## Impact

- **New code:** `src/Bank.Domain`, `src/Bank.Application`, `src/Bank.Data` and `src/Bank.Api`, with `spec/Bank.*.Specs` for each, plus `BankApi.slnx`, `global.json` and `README.md` at the repo root.
- **Dependencies:** .NET 10 SDK (10.0.111 is installed), xunit.v3 4.x, Shouldly 4.x and Microsoft.AspNetCore.Mvc.Testing 10.x. There is no FastEndpoints, no mediator library and no NSpec.
- **Files:** `mable_account_balances.csv` and `mable_transactions.csv` stay unchanged. At runtime only the build-output copy of the balances file is written.
- **HTTP surface:** `POST /transfer-batches` and `GET /accounts` at `http://localhost:5080`.
