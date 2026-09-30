# Design Summary

The agreed design for the Mable back end code challenge, from the design interview on 30/09/2026. See [design-interview.md](./design-interview.md) for the reasoning and research behind each decision. Domain terms follow [CONTEXT.md](../CONTEXT.md).

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
tests/Bank.Domain.Specs/
tests/Bank.Application.Specs/
tests/Bank.Data.Specs/
tests/Bank.Api.Specs/
```

## Domain (written first, test-first)

```
                 ┌─────────────────────────────────────────────┐
                 │ TransferBatch                               │
                 │  Transfers : [Transfer]  (in Position order)│
                 │                                             │
                 │  Settle(Accounts) → SettlementResult        │
                 │   • passes, ordering, Account lookup        │
                 │   • never reads a Balance itself            │
                 └─────┬──────────────────┬───────────┬────────┘
                       │ 1..*             │ uses      │ returns
                       ▼                  │           ▼
 ┌─────────────────────────────────────┐  │  ┌──────────────────────────────┐
 │ Transfer                            │  │  │ SettlementResult             │
 │  SendingAccount   : AccountNumber   │  │  │  Settled  : [Transfer]       │
 │  ReceivingAccount : AccountNumber   │  │  │    (in settle order)         │
 │  Requested : decimal (any sign)     │  │  │  Rejected : [Transfer]       │
 │  Amount    : Money   (only if > 0)  │  │  │    (each with its reason)    │
 │  Position  : int     (1-based)      │  │  │  Accounts : Accounts         │
 │  Status    : TransferStatus         │  │  └──────────────────────────────┘
 │  Reason    : RejectionReason?       │  │
 │                                     │  ▼
 │  constructor   own-field rules:     │  ┌──────────────────────────────────┐
 │   1. Requested ≤ 0                  │  │ Accounts                         │
 │        → Rejected(NonPositiveAmount)│  │  Dictionary<AccountNumber,       │
 │   2. Sending == Receiving           │  │             Account>             │
 │        → Rejected(SameAccount)      │  │  ctor: refuses duplicate numbers │
 │                                     │  │  Find(AccountNumber) → Account?  │
 │  SettleBetween(sending, receiving)  │  └────────────────┬─────────────────┘
 │   sending.TryWithdraw(Amount)       │                   │ 0..*
 │    true  → receiving.Deposit        │                   ▼
 │            → Settled                │  ┌──────────────────────────────────┐
 │    false → stays Unsettled          │─►│ Account  (entity)                │
 └──────┬──────────────────────────────┘  │  Number  : AccountNumber         │
        │                                 │  Balance : Money                 │
        ▼                                 │  TryWithdraw(Money) → bool       │
 ┌─────────────────────────────┐          │   false if Balance would go < $0 │
 │ TransferStatus (enum)       │          │  Deposit(Money)                  │
 │  Settled                    │          └──────┬────────────────────┬──────┘
 │  Unsettled (when created)   │                 ▼                    ▼
 │  Rejected (sets Reason)     │   ┌────────────────────┐ ┌─────────────────────┐
 └──────┬──────────────────────┘   │ AccountNumber (VO) │ │ Money (VO)          │
        ▼                          │  string, 16 digits │ │  decimal, ≥ 0, ≤ 2dp│
 ┌───────────────────────────────┐ └────────────────────┘ │  AUD assumed        │
 │ RejectionReason, precedence   │                        └─────────────────────┘
 │  1. NonPositiveAmount   ctor  │
 │  2. SameAccount         ctor  │ ┌──────────────────────────────────┐
 │  3. UnknownSendingAccount     │ │ «interface» IAccountRepository   │
 │  4. UnknownReceivingAccount   │ │  GetAll()          → Accounts    │
 │  5. InsufficientFunds   end   │ │  SaveAll(Accounts)               │
 └───────────────────────────────┘ └──────────────────────────────────┘
                                     implemented in Data (FileAccountRepository)
```

How `Settle` runs:

```
 Pass 1, in Position order:
   Rejected when created?     → skip, and report as Rejected
   Find(Sending) is null      → Rejected(UnknownSendingAccount)
   Find(Receiving) is null    → Rejected(UnknownReceivingAccount)
   else SettleBetween(...)    → Settled | stays Unsettled

 Pass 2..n: retry the Unsettled Transfers, in Position order
 Stop when a Pass settles nothing → the rest are Rejected(InsufficientFunds)
```

- **`AccountNumber`** (value object): exactly 16 digits, stored as a string.
- **`Money`** (value object): non-negative, at most 2 decimal places, single currency (AUD assumed).
- **`Account`** (entity): has an `AccountNumber` and a balance (`Money`, so never negative). It owns the $0 floor, and is the only place that rule lives:
  - `TryWithdraw(Money)` returns `false` and changes nothing when the withdrawal would take the balance below $0.
  - `Deposit(Money)` always succeeds.
- **`Transfer`**: has a `SendingAccount` and a `ReceivingAccount` (both `AccountNumber`s), a requested amount, a Position and a status.
  - The **Position** is the Transfer's 1-based place in the Transfer Batch. The domain never sees CSV line numbers.
  - The **requested amount** is a `decimal` of any sign, exactly as the Company wrote it, so a Rejected Transfer can always report it.
  - The `Transfer` constructor checks the rules that need only the Transfer itself, and sets the status to **Rejected** with the first that fails:
    1. `NonPositiveAmount`: the requested amount is ≤ 0.
    2. `SameAccount`: the Sending and Receiving Account are the same.
  - Otherwise the Transfer also holds the amount as `Money`. That `Money` is only created once the amount is known to be positive, so it can't fail.
  - The Transfer owns its status. `SettleBetween(sending, receiving)` calls `sending.TryWithdraw(amount)`: on success it deposits to the Receiving Account and becomes **Settled**; otherwise it stays **Unsettled**. A Transfer that passes the constructor's checks starts **Unsettled**.
  - `TransferStatus` is a plain enum (`Unsettled`, `Settled`, `Rejected`). A Rejected Transfer's reason is in its `Reason` property, which is null otherwise.
- **`Accounts`**: the Company's Accounts, backed by a `Dictionary<AccountNumber, Account>`.
  - The constructor refuses a list that holds the same Account number twice.
  - `Find(AccountNumber)` returns the Account, or null when it's unknown.
- **`TransferBatch`**: the Transfers in submission order. `Settle(Accounts)` does gross, multi-pass settlement ([ADR 0001](../adr/0001-gross-multi-pass-settlement.md)):
  - **Pass 1** walks the Transfers in order.
    - Already Rejected at creation → skipped, and reported with the other Rejected Transfers.
    - `accounts.Find` returns null for the Sending Account → **Rejected** (`UnknownSendingAccount`); otherwise null for the Receiving Account → **Rejected** (`UnknownReceivingAccount`). Never retried.
    - Otherwise the batch calls `transfer.SettleBetween(sending, receiving)`, which Settles it or leaves it Unsettled. The batch never reads a Balance itself.
  - **Later passes** retry the Unsettled Transfers in their original order.
  - Settlement **stops** when a pass settles nothing. Anything still Unsettled is Rejected as `InsufficientFunds`.
  - **Returns a `SettlementResult`:**
    - `Settled`: the Settled Transfers, in the order they settled.
    - `Rejected`: the Rejected Transfers, each with its Rejection Reason.
    - `Accounts`: the updated Accounts.
- **Rejection reasons**, in precedence order: `NonPositiveAmount`, `SameAccount`, `UnknownSendingAccount`, `UnknownReceivingAccount`, `InsufficientFunds`. A Rejected Transfer reports only the first reason that applies.
- **`IAccountRepository`**: `GetAll()` returns `Accounts`, and `SaveAll(Accounts)` writes them back.

## Application

- **`TransferCsvParser`**: the only place the CSV is checked. It turns CSV text into `TransferRow`s (from, to and amount as primitives, plus the line number; `from`/`to` are the file's words, and the command maps them to the Sending and Receiving Account) or into line-numbered errors.
  - The format is 3 columns with **no header row**, and CRLF and LF line endings are both accepted.
  - Errors include a wrong column count, an account number that isn't 16 digits, and an amount that isn't numeric or has more than 2 decimal places.
  - Any sign is allowed for the amount; the domain decides what to do with it.
  - The command maps each row's line number to the Transfer's Position. They're always equal: there's no header row, and any parse error rejects the whole file.
  - There is no separate validator.
- **`SettleTransferBatchCommand` + handler**:
  1. Parse the CSV; any error → an error result.
  2. Map the rows to domain Transfers, so the CSV never reaches the domain.
  3. Load the Accounts, call `batch.Settle(accounts)` with the `Accounts` from the repository, then save.
  4. Return the `SettlementResult`.
  - A plain C# `lock` around load, settle and save makes sure only one settlement runs at a time. The get-accounts query takes no lock.
- **Get-accounts query + handler**: returns the current balances.

## Data

- **`FileAccountRepository`** implements `IAccountRepository`. It reads and writes a balances CSV (`account,balance`, no header) at a path passed in when the Api registers it.
- The csproj copies `mable_account_balances.csv` (repo root) into the build output (`CopyToOutputDirectory=PreserveNewest`), and the repository works on that copy. The file in the repo is never modified, and `dotnet clean` resets the balances.
- There's no seed-copy or safe-write logic. A malformed balances file stops the app at startup, and so does one that lists an account twice, because building `Accounts` refuses it.

## Api (Minimal API, no mediator library)

- **`POST /transfer-batches`**: `multipart/form-data` with one CSV file. The endpoint injects the Application handler and calls it directly.
  - **200:** the CSV is well-formed. This holds even if every Transfer is Rejected.
    `line` in the response is the Transfer's Position, named for the person reading the file.
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
- **Style:** nested classes read like describe/context/it, e.g. `TransferBatchSpec` → `Settle` → `when_the_sending_account_is_short_but_receives_funds_later` → `it_settles_on_a_later_pass`.
- **Orthogonal:** each project specifies only its own layer.
  - **Domain:** pure, no fakes. Value object rules, Account overdraft guard, every rejection reason and their precedence order, retrying Unsettled Transfers and later settlement, knock-on failures, cycles, order-dependence, stopping.
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
  - A `GET /accounts` that arrives while a settlement is writing the balances file can fail.
  - Outcomes depend on file order (see ADR 0001).

## Delivery plan

The work is delivered as one OpenSpec change. Its `tasks.md` has four task groups, in this order, and each group lands its own specs:

| # | Task group | Covers |
|---|---|---|
| 1 | Domain | `BankApi.slnx` and `global.json` from the [Solution layout](#solution-layout), then [Domain](#domain-written-first-test-first): value objects, `Account`, `Accounts`, `Transfer`, `TransferBatch`, `SettlementResult`, `IAccountRepository`, and the domain specs. Test-first. |
| 2 | Application | [Application](#application): `TransferCsvParser`, the settle command and get-accounts query with their handlers, and the Application specs. |
| 3 | Data | [Data](#data): `FileAccountRepository`, copying the balances CSV into the build output, and the Data specs. |
| 4 | Api and README | [Api](#api-minimal-api-no-mediator-library): both endpoints and hosting, the Api specs, the headline acceptance spec, and the [README](#readme-contents). |

Each group adds only its own layer's `src/` and `tests/` projects to the solution.

## Before building

The machine currently has only the .NET 10 runtime. Install the SDK first: `sudo pacman -S dotnet-sdk`.
