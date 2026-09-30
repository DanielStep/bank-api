# Design

## Context

The repo holds no code yet. [docs/scoping/design-summary.md](../../../../docs/scoping/design-summary.md) is the design this change builds, and it is authoritative for the solution layout, the domain model, how `Settle` runs, the Application, Data and Api layers, and the spec style. This document does not repeat it. It records only the decisions the summary leaves open, plus one amendment the user made while this change was being proposed. For the motivation, see [proposal.md](./proposal.md) (Why), and for behaviour, see [specs/bank-api/spec.md](./specs/bank-api/spec.md).

Environment: .NET SDK 10.0.111 is now installed at `/usr/share/dotnet/sdk`, so the summary's "Before building" step is already done.

## Goals / Non-Goals

**Goals:**
- Settle every decision that would otherwise be left to whoever implements a task, so the tasks can be followed without redesign.
- Keep each layer's specs orthogonal, as the summary requires. The design choices below are the ones that make that possible, for example how the Api specs swap in a temporary balances file.

**Non-Goals:**
- Changing anything the summary settles, beyond the Unsettled amendment (D1).
- Idempotency, more than one Company, and running Settlements in parallel. The README lists all three as known limitations.

## Decisions

### D1. Unsettled replaces Deferred (amends the summary)
`TransferStatus` is `Unsettled`, `Settled` or `Rejected`. The `Transfer` constructor gives a Transfer that is either Rejected (the `NonPositiveAmount` and `SameAccount` checks) or Unsettled. `SettleBetween` leaves it Unsettled when `TryWithdraw` returns false. `TransferBatch.Settle` runs Pass 1 over every Unsettled Transfer, then keeps retrying the Unsettled ones until a Pass settles nothing, and finally Rejects whatever is still Unsettled as `InsufficientFunds`.
- *Why:* the summary's three statuses had no state for a Transfer that has passed its own checks but has not yet been tried. The user chose to replace Deferred rather than add a fourth status, so each status now means one thing.
- *Alternatives:* adding Unsettled next to Deferred (a status for an untried Transfer as well as one for a short one, and both mean "try again"), or a nullable status (every caller would have to handle null).
- CONTEXT.md, the design summary, ADR 0001, `openspec/config.yaml` and the interview log (Q34) have been updated to match.

`TransferStatus` is a plain enum. A Rejected Transfer's reason is held in `Transfer.Reason`, a nullable `RejectionReason` that is set only when the Transfer is Rejected. `RejectionReason` is an enum declared in precedence order. The Transfer is built with an ordinary constructor, not a static `Create`, like the value objects (D4).
- *Why:* an enum plus one nullable property is simpler to read than an abstract record with sealed subtypes.

### D2. SettlementResult orders its Rejected Transfers by Position
`Settled` keeps the order in which Transfers settled, as the summary says. `Rejected` is sorted by Position (the user's decision), so a Transfer Rejected when it was created on line 4 is listed after one Rejected for `InsufficientFunds` on line 2. The sort happens in the domain, so the Api just maps the lists in order.

### D3. Accounts enumerate in the order they were given
`Accounts` keeps the `Dictionary<AccountNumber, Account>` the summary asks for, for `Find`, and also keeps the constructor's list, exposed as `All`, so that it lists the Accounts in balances-file order. The Api's `balances` list, `GET /accounts` and the rewritten balances file all follow that order.
- *Why:* insertion order is not part of `Dictionary`'s contract, but the specs promise file order.
- *Alternative:* the generic `OrderedDictionary<TKey,TValue>` in .NET 9 and later. It was rejected because the summary names `Dictionary`.

### D4. Value objects validate in their constructors, and the parser reuses the rules
- `new AccountNumber(string)` throws `ArgumentException` unless the value is exactly 16 ASCII digits. `AccountNumber.IsValid(string)` checks the same rule without throwing. `AccountNumber` is a `readonly record struct` with value equality, so it works as a dictionary key.
- `new Money(decimal)` throws `ArgumentException` when the value is below 0 or has a scale above 2. `Money.IsValid(decimal)` checks the same rule without throwing. `Money` has `Add` and `Subtract` methods, and `Subtract` is used only after `TryWithdraw` has checked the Balance.
- *Why constructors:* a throwing constructor plus a static `IsValid` is simpler to read than static `Create`/`TryCreate` factories, and keeps each rule in one place.
- "More than 2 decimal places" means the parsed `decimal`'s **scale**, so `5.100` is refused and never rounded, in line with Q7.
- `TransferCsvParser` checks account numbers with `AccountNumber.IsValid`, so the 16-digit rule lives in one place. It parses amounts itself, because a Transfer's amount may be negative and `Money` may not.

### D5. How strictly the CSVs are parsed
Both CSV readers (the Transfer parser and the balances file) work the same way:
- They split on `\n` and strip one trailing `\r` from each line.
- A single empty last element (the file's final line ending) is ignored. Any other empty line is a wrong-column-count error. This keeps line number and Position equal.
- They split each line on `,`, with no quoting and no whitespace trimming.
- They parse numbers with `decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, …)`, so there are no thousands separators, no exponents, no currency symbols and no spaces.
- A file with no Transfers produces one error, on line 1: "The file holds no Transfers".
- The parser reports **every** malformed line, not just the first.

### D6. Application result types
- `TransferCsvParser.Parse(string)` returns a `ParseResult` with two lists, `Rows` (`TransferRow`s) and `Errors` (`CsvError`s, each with an `int Line` and a `string Message`). When `Errors` is not empty, `Rows` is empty.
- `SettleTransferBatchHandler.Handle(SettleTransferBatchCommand)` returns a `SettleTransferBatchOutcome` with a nullable `Result` (`SettlementResult`) and an `Errors` list. `Result` is null exactly when `Errors` is not empty.
- Both are plain classes, and callers check `Errors.Count > 0`. There is no Result library.
- *Why plain classes:* simpler to read than abstract records with sealed subtypes and a `switch`, in line with D1 and D4.
- The command carries the CSV text as a `string`. The endpoint reads the uploaded file with a `StreamReader`, so the Application layer never sees `IFormFile`.

### D7. A plain `lock` in the settle handler
`SettleTransferBatchHandler` holds a `private static readonly object` and wraps `GetAll` through `SaveAll` in a C# `lock` statement, so only one Settlement runs at a time and none of its changes is lost. The code is synchronous, so `lock` is enough and no `SemaphoreSlim` is needed.
- The get-accounts handler takes no lock.
- The lock has no Application spec of its own. The Api's "Two uploads arrive together" spec (task 4.4) proves it.
- *Why:* the user chose the simplest form that still meets "One Settlement runs at a time", over a registered `AccountsLock` wrapper shared by both handlers.
- *Trade-off:* a `GET /accounts` that lands during `SaveAll` could read a half-written file and fail with 500. This is rare, and the README lists it as a known limitation.

### D8. FileAccountRepository reads on every call, fails loudly and never caches
- `GetAll()` reads and parses the whole file each time. It builds `Accounts`, which refuses a duplicate number.
- Any bad line or duplicate throws `InvalidDataException`, and the message names the file path and the line or the account number.
- `SaveAll` writes `account,balance` lines with `balance.ToString("0.00", CultureInfo.InvariantCulture)`, in `Accounts` order, with `\n` line endings, using `File.WriteAllText`. The summary rules out a temp file and rename.

### D9. Where the balances file comes from
- `Bank.Data.csproj` includes `..\..\mable_account_balances.csv` with `Link="mable_account_balances.csv"` and `CopyToOutputDirectory=PreserveNewest`.
- The copy flows through the `ProjectReference` into `src/Bank.Api/bin/.../mable_account_balances.csv`, and this is checked in task 3.
- The Api registers `IAccountRepository` as `new FileAccountRepository(Path.Combine(AppContext.BaseDirectory, "mable_account_balances.csv"))`.
- *Why Data's csproj:* the summary puts this under Data, and the Delivery plan lands it in task group 3, before the Api project exists.

### D10. Startup check
`Program.cs` builds the app, resolves `IAccountRepository` and calls `GetAll()` once before `app.Run()`. A malformed or duplicate balances file therefore throws at startup and the process exits non-zero, with the `InvalidDataException` message on the console.

### D11. Port 5080 without launch profiles
- `appsettings.json` sets `"Urls": "http://localhost:5080"`.
- There is no `Properties/launchSettings.json`, so `dotnet run` can't override the URL with a launch profile, and `dotnet Bank.Api.dll` behaves the same.
- There is no `UseHttpsRedirection` and no HTTPS endpoint.

### D12. HTTP contract details
- **Upload field:** the form field is named `file` (`curl -F file=@mable_transactions.csv`), and the endpoint binds it as `IFormFile file` with `.DisableAntiforgery()`. A request without it gets 400 from Minimal API binding, and the Api spec checks this.
- **400 body:** `TypedResults.Problem(statusCode: 400, title: "The CSV file is malformed", extensions: { ["errors"] = [{ "line": 2, "message": "…" }, …] })`.
- **200 body:** the Api's own response records `TransferBatchResponse(Settled, Rejected, Balances)`, `TransferLine(Line, From, To, Amount)`, `RejectedLine(… , Reason)` and `BalanceLine(AccountNumber, Balance)`, serialised with the default web (camelCase) options.
  - `amount` is the Transfer's requested `decimal`. `reason` is `RejectionReason.ToString()`.
  - `System.Text.Json` keeps a `decimal`'s scale, so `500.00` serialises as `500.00`. The Api spec asserts this on the raw JSON.
- **`GET /accounts`:** a JSON array of `BalanceLine`, the same element shape as `balances`.

### D13. Api specs use their own balances file
Each Api spec copies the balances fixture it needs to a temporary file. It then builds a `WebApplicationFactory<Program>` whose `ConfigureTestServices` replaces `IAccountRepository` with a `FileAccountRepository` on that temporary file, so the startup check (D10) runs against it too. This needs no configuration key, so "nothing to configure" still holds. The headline acceptance spec reads the repo's real `mable_account_balances.csv` and `mable_transactions.csv`, found by walking up from `AppContext.BaseDirectory` to the directory that holds `BankApi.slnx`.

### D14. Packages and project settings
- **Packages:** `xunit.v3` 4.x, `Shouldly` 4.3.x and, for Api specs only, `Microsoft.AspNetCore.Mvc.Testing` 10.x. There is no `Microsoft.NET.Test.Sdk` and no `xunit.runner.visualstudio` (ADR 0002).
- **Project settings:** every project uses `net10.0`, `Nullable` enabled, `ImplicitUsings` enabled and `TreatWarningsAsErrors`. Spec projects set `OutputType=Exe`, which xunit.v3 needs.
- **Shared file:** a root `Directory.Build.props` holds the shared properties.
- **Versions:** each package's version is pinned in its csproj. There is no central package management, because four spec projects aren't worth it.

## Risks / Trade-offs

- **The spec step "Settled in the order: …" is observable only through settle order.** The Pass number itself is never exposed. → The domain specs assert on `SettlementResult.Settled` order, which is enough to tell Pass 1 from later Passes.
- **Some scenarios can't be automated in-process:** "fresh clone is run", "survives a restart" and "a clean build resets". → Task 4 checks them by hand with `dotnet run`, `dotnet clean` and `curl`, and records the commands in the README.
- **`PreserveNewest` depends on timestamps.** If someone edits the repo's balances file, the next build overwrites the working copy and loses the Settled Balances. → This is intended (the repo file is the seed). The README notes it next to `dotnet clean`.
- **The concurrency spec is timing-dependent.** → It starts both uploads before awaiting either, with `Task.WhenAll`, and asserts only the final Balances, which are the same whatever order the uploads run in.
- **Scale-based rejection surprises people:** `5.100` is refused. → The 400 message says "more than 2 decimal places", and D4 records why.
