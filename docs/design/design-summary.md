# Design Summary

The agreed design for the Mable back end code challenge, from the design interview on 30/09/2026. See [design-interview.md](./design-interview.md) for the reasoning and research behind each decision. Domain terms follow [CONTEXT.md](../../CONTEXT.md).

## Goal

A .NET 10 HTTP API that holds one Company's Account balances and settles a day's Transfer Batch uploaded as CSV. It must run on Windows and Mac with only `dotnet run`, and nothing to configure.

Reviewer rubric to satisfy:
- **Data structure:** uses domain models, and uses native data structures readably.
- **Tests:** good coverage, orthogonal, and they explain the functionality.
- **Object orientation:** models encapsulate logic, concerns are separated, and methods are short and readable.
- **General:** it runs and provides feedback, and it calculates the sample files accurately.

## Solution layout

```
BankApi.slnx
global.json                      SDK 10.0 with roll-forward; test runner = Microsoft.Testing.Platform
src/Bank.Domain/                 no dependencies
src/Bank.Application/            → Domain
src/Bank.Data/                   → Domain
src/Bank.Api/                    → Application, Data (composition root)
spec/Bank.Domain.Specs/
spec/Bank.Application.Specs/
spec/Bank.Data.Specs/
spec/Bank.Api.Specs/
```

## Domain (written first, test-first)

- **`AccountNumber`** (value object): exactly 16 digits, stored as a string.
- **`Money`** (value object): non-negative, at most 2 decimal places, single currency (AUD assumed).
- **`Account`** (entity): has an `AccountNumber` and a balance (`Money`, so never negative). It takes money in and out, and refuses any withdrawal that would take the balance below $0.
- **`Transfer`**: has a from Account number, a to Account number, a requested amount, a line number and a status.
  - The **requested amount** is a `decimal` of any sign, exactly as the Company wrote it, so a Rejected Transfer can always report it.
  - `Transfer.Create(...)` sets the status to **Rejected** (`NonPositiveAmount`) when the requested amount is ≤ 0.
  - Otherwise the Transfer also holds the amount as `Money`. That `Money` is only created once the amount is known to be positive, so it can't fail.
  - The Transfer owns its status; `Settle` moves it between Deferred, Settled and Rejected.
- **`TransferBatch`**: the Transfers in submission order. `Settle(accounts)` does gross, multi-pass settlement ([ADR 0001](../adr/0001-gross-multi-pass-settlement.md)):
  - **Pass 1** walks the Transfers in order.
    - Already Rejected at creation → skipped, and reported with the other Rejected Transfers.
    - Unknown sending Account, unknown receiving Account, or the same Account on both sides → **Rejected** straight away, never retried.
    - The sender is short of funds → **Deferred**.
    - Otherwise → **Settled**.
  - **Later passes** retry the Deferred Transfers in their original order.
  - Settlement **stops** when a pass settles nothing. Anything still Deferred is Rejected as `InsufficientFunds`.
  - **Result:** the Settled Transfers (in the order they settled), the Rejected Transfers with reasons, and the updated Accounts.
- **Rejection reasons:** `UnknownSendingAccount`, `UnknownReceivingAccount`, `SameAccount`, `NonPositiveAmount`, `InsufficientFunds`.
- **`IAccountRepository`**: `GetAll()` and `SaveAll(accounts)`.

## Application

- **`TransferCsvParser`**: the only place the CSV is checked. It turns CSV text into `TransferRow`s (from, to and amount as primitives, plus the line number) or into line-numbered errors.
  - The format is 3 columns with **no header row**, and CRLF and LF line endings are both accepted.
  - Errors include a wrong column count, an account number that isn't 16 digits, and an amount that isn't numeric or has more than 2 decimal places.
  - Any sign is allowed for the amount; the domain decides what to do with it.
  - There is no separate validator.
- **`SettleTransferBatchCommand` + handler**:
  1. Parse the CSV; any error → an error result.
  2. Map the rows to domain Transfers, so the CSV never reaches the domain.
  3. Load the Accounts, call `batch.Settle(accounts)`, then save.
  4. Return the outcome.
  - A `SemaphoreSlim` makes sure only one settlement runs at a time.
- **Get-accounts query + handler**: returns the current balances.

## Data

- **`FileAccountRepository`** implements `IAccountRepository`. It reads and writes a balances CSV (`account,balance`, no header) at a path passed in when the Api registers it.
- The csproj copies `mable_account_balances.csv` (repo root) into the build output (`CopyToOutputDirectory=PreserveNewest`), and the repository works on that copy. The file in the repo is never modified, and `dotnet clean` resets the balances.
- There's no seed-copy or safe-write logic. A malformed balances file, or one that lists an account twice, stops the app at startup.

## Api (Minimal API, no mediator library)

- **`POST /transfer-batches`**: `multipart/form-data` with one CSV file, and `.DisableAntiforgery()`. The endpoint injects the Application handler and calls it directly.
  - **200:** the CSV is well-formed. This holds even if every Transfer is Rejected.
    ```json
    {
      "settled":  [{ "line": 1, "from": "...", "to": "...", "amount": 500.00 }],
      "rejected": [{ "line": 4, "from": "...", "to": "...", "amount": 25.60, "reason": "InsufficientFunds" }],
      "balances": [{ "accountNumber": "...", "balance": 4820.50 }]
    }
    ```
  - **400:** a parse error, returned as ProblemDetails with line-numbered errors. Nothing is applied.
- **`GET /accounts`**: the current balances.
- **Hosting:** HTTP only (no HTTPS redirect, no dev cert) on a fixed port other than 5000. Paths are built from the content root or base directory.
- **Company:** there's one Company per deployment; no Company in the model or routes.

## Specs (xUnit v3 4.x + Shouldly)

[ADR 0002](../adr/0002-xunit-and-shouldly-in-place-of-rspec.md) records why these replace RSpec.
- **Style:** nested classes read like describe/context/it, e.g. `TransferBatchSpec` → `Settle` → `when_the_sender_is_short_but_receives_funds_later` → `it_settles_on_a_later_pass`.
- **Orthogonal:** each project specifies only its own layer.
  - **Domain:** pure, no fakes. Value object rules, Account overdraft guard, every rejection reason, deferral and later settlement, knock-on failures, cycles, order-dependence, stopping.
  - **Application:** parser rows and errors; the command with an in-memory `IAccountRepository` fake, checking that it maps, calls the domain and saves (without re-testing the settlement rules).
  - **Data:** the file repository against a temporary file: round-trip and malformed-file handling.
  - **Api:** `WebApplicationFactory`, in-process, each spec on its own temporary balances file: 200 on the happy path, 400 on a parse error, `GET /accounts`.
- **Headline acceptance spec:** the sample files give these closing balances.

| Account | Closing |
|---|---:|
| 1111234522226789 | 4,820.50 |
| 1111234522221234 | 9,974.40 |
| 2222123433331212 | 1,550.00 |
| 1212343433335665 | 1,725.60 |
| 3212343433335755 | 48,679.50 |

## README contents

- `dotnet run --project src/Bank.Api`, then one `curl -F file=@mable_transactions.csv …` line.
- The expected closing balances (the table above), and `dotnet test` to run the specs.
- A note that .NET 10 was used, as agreed with Mable.
- **Known limitations:**
  - Re-posting a file settles it again (no idempotency).
  - One Company only.
  - One settlement runs at a time.
  - Outcomes depend on file order (see ADR 0001).

## Before building

The machine currently has only the .NET 10 runtime. Install the SDK first: `sudo pacman -S dotnet-sdk`.
