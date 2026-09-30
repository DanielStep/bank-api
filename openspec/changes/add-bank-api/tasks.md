# Tasks

Every task is test-first. Write the failing spec (xUnit v3 + Shouldly, nested describe/context/it classes as in ADR 0002), watch it fail for the right reason, then write the code that makes it pass. "Verify" means `dotnet test` is green for the named spec project unless stated otherwise.

## 1. Domain

- [x] 1.1 Create `global.json` (SDK 10.0 with `rollForward: latestFeature`, `test.runner: Microsoft.Testing.Platform`), `Directory.Build.props` (D14), `BankApi.slnx`, `src/Bank.Domain` (class library, no references) and `spec/Bank.Domain.Specs` (xunit.v3 4.x + Shouldly, `OutputType=Exe`) with one placeholder spec. Verify: `dotnet build BankApi.slnx` succeeds and `dotnet test` runs and passes the placeholder spec.
- [x] 1.2 Spec then build `AccountNumber` (D4): accepts exactly 16 digits, keeps leading zeros, refuses 15 digits, 17 digits and non-digits, and compares by value. Verify: the `AccountNumberSpec` contexts pass.
- [x] 1.3 Spec then build `Money` (D4): accepts $0.00 and 2-dp values, refuses negative values and a scale above 2 (including `5.100`), and adds and subtracts. Verify: the `MoneySpec` contexts pass.
- [x] 1.4 Spec then build `Account`: `TryWithdraw` succeeds down to exactly $0.00; one cent short returns false and leaves the Balance unchanged; `Deposit` always adds. Verify: the `AccountSpec` contexts pass.
- [x] 1.5 Spec then build `Accounts` (D3): `Find` returns the Account or null, enumerates in constructor order, and the constructor refuses a duplicate account number. Verify: the `AccountsSpec` contexts pass.
- [x] 1.6 Spec then build `RejectionReason`, `TransferStatus` (Unsettled, Settled, Rejected; D1) and the `Transfer` constructor:
  - `NonPositiveAmount` for $0.00 and −$5.00, and `SameAccount`; `NonPositiveAmount` wins when both apply.
  - Otherwise the Transfer is Unsettled and holds `Money`.
  - The requested amount is kept exactly as given.
  - Verify: the `TransferSpec` creation contexts pass.
- [x] 1.7 Spec then build `Transfer.SettleBetween`: with enough funds it moves the amount and becomes Settled; when short it moves nothing and stays Unsettled. Verify: the `TransferSpec` → `SettleBetween` contexts pass.
- [x] 1.8 Spec then build `TransferBatch.Settle` Pass 1 and `SettlementResult`, covering the `bank-api` spec's scenarios for the $0 floor, Position order on Pass 1, the rule Rejections (the outline, including a Transfer to an unknown Account that is not retried after the Sending Account is funded) and precedence (the outline). Verify: those `TransferBatchSpec` → `Settle` contexts pass.
- [x] 1.9 Spec then extend `Settle` with later Passes and stopping, covering the `bank-api` spec's scenarios:
  - receives funds later
  - overtaking
  - Unsettled Transfers retried in Position order
  - knock-on effects
  - cycle
  - a Pass that settles something is followed by another Pass
  - Verify: those contexts pass, and a spec with N mutually dependent Transfers finishes.
- [x] 1.10 Spec then build the `SettlementResult` ordering (D2): Settled in settle order and Rejected in Position order, each with its reason and requested amount, plus the closing Accounts ("Outcomes are reported in their documented order"). Verify: the context passes.
- [x] 1.11 Add the `IAccountRepository` interface (`GetAll() → Accounts`, `SaveAll(Accounts)`) to `Bank.Domain`; it has no behaviour to spec. Verify: `dotnet build` succeeds and all `Bank.Domain.Specs` pass.

## 2. Application

- [x] 2.1 Create `src/Bank.Application` (→ Domain) and `spec/Bank.Application.Specs`, and add both to `BankApi.slnx`. Verify: `dotnet build BankApi.slnx` succeeds.
- [x] 2.2 Spec then build the happy paths of `TransferCsvParser` and `ParseResult` (D5, D6): the sample file gives four `TransferRow`s with line numbers 1–4; LF, CRLF, and a last line with or without a line ending all parse; `-5.00` and `+5` parse. Verify: the `TransferCsvParserSpec` → `when_the_file_is_well_formed` contexts pass.
- [x] 2.3 Spec then build the `TransferCsvParser` errors (D5), one context per example in the `bank-api` spec's "A line is malformed" outline:
  - two fields, four fields
  - a 15-digit account, a 17-digit account, a non-digit account
  - a non-numeric amount, a 3-dp amount, an empty amount
  - a blank line in the middle
  - Also cover: every malformed line reported; an empty file and a file holding only a line ending each give one error on line 1.
  - Verify: the `when_the_file_is_malformed` contexts pass.
- [ ] 2.4 Spec then build `AccountsLock` (D7) and `SettleTransferBatchCommand` + handler + `SettleTransferBatchOutcome` (D6), against an in-memory `IAccountRepository` fake. Cover:
  - a malformed CSV returns an outcome with `Errors` and no `Result`, and never calls `SaveAll`;
  - a well-formed CSV maps line → Position and from/to → Sending/Receiving Account, calls `Settle`, saves the resulting Accounts once, and returns an outcome with the `Result` and no `Errors`.
  - Don't re-spec the settlement rules.
  - Verify: the `SettleTransferBatchHandlerSpec` contexts pass.
- [ ] 2.5 Spec then build the get-accounts query + handler: it returns the fake's Accounts in order and takes the lock (D7). Verify: the `GetAccountsHandlerSpec` contexts pass and all `Bank.Application.Specs` pass.

## 3. Data

- [ ] 3.1 Create `src/Bank.Data` (→ Domain) and `spec/Bank.Data.Specs`, add both to `BankApi.slnx`, and add the linked `mable_account_balances.csv` with `CopyToOutputDirectory=PreserveNewest` (D9). Verify: after `dotnet build`, `src/Bank.Data/bin/Debug/net10.0/mable_account_balances.csv` exists and `git status` shows the repo's CSV unchanged.
- [ ] 3.2 Spec then build `FileAccountRepository.GetAll` (D8) against a temporary file:
  - the five sample Accounts, in file order;
  - an account with leading zeros;
  - LF and CRLF line endings;
  - each malformed example from the `bank-api` spec's "The balances file is malformed" outline (wrong field count, 15 digits, non-numeric, −0.01, 3 dp) and a duplicate account, each throwing `InvalidDataException` naming the file (and the account number, for the duplicate).
  - Verify: the `FileAccountRepositorySpec` → `GetAll` contexts pass.
- [ ] 3.3 Spec then build `FileAccountRepository.SaveAll` (D8): a round trip keeps the Balances and order, and the written text is exactly `1111111111111111,99.50\n2222222222222222,0.50\n` for the `bank-api` spec's "The working balances file keeps its format" scenario. Verify: the `SaveAll` contexts pass and all `Bank.Data.Specs` pass.

## 4. Api and README

- [ ] 4.1 Create `src/Bank.Api` (web, → Application and Data) with `appsettings.json` `Urls` = `http://localhost:5080`, no `launchSettings.json` and no HTTPS redirection (D11). Wire up the DI (repository on `AppContext.BaseDirectory`, `AccountsLock`, handlers) and the startup `GetAll()` check (D10). Create `spec/Bank.Api.Specs` with `Microsoft.AspNetCore.Mvc.Testing` and the temporary-file factory (D13), and add both to `BankApi.slnx`. Verify: `dotnet build` succeeds and `src/Bank.Api/bin/Debug/net10.0/mable_account_balances.csv` exists.
- [ ] 4.2 Spec then build `GET /accounts` (D12), covering the `bank-api` spec's balance scenarios:
  - opening Balances in file order as a JSON array of `accountNumber` strings and `balance` numbers;
  - leading zeros kept;
  - Balances after an upload;
  - the service refuses to start on a malformed or duplicate balances file.
  - Verify: the `GetAccountsSpec` contexts pass.
- [ ] 4.3 Spec then build `POST /transfer-batches` (D12), covering the `bank-api` spec's upload scenarios:
  - the sample upload gives 200;
  - no file gives 400;
  - line endings;
  - a negative amount gives 200 with `NonPositiveAmount`;
  - a malformed line gives a 400 problem details body with line-numbered `errors` and unchanged Balances;
  - no Transfers gives 400;
  - the mixed batch's exact `settled`/`rejected`/`balances` JSON (asserting `500.00`-style numbers on the raw body);
  - every Transfer Rejected still gives 200;
  - the same file uploaded twice gives $4,641.00.
  - Verify: the `PostTransferBatchesSpec` contexts pass.
- [ ] 4.4 Spec the `bank-api` spec's "Two uploads arrive together" scenario: two uploads of the sample file started together with `Task.WhenAll` both return 200, and 1111234522226789 ends at $4,641.00. Verify: the context passes.
- [ ] 4.5 Write the headline acceptance spec: the real `mable_account_balances.csv` + `mable_transactions.csv` settle all four lines in order, reject none, and give the five closing Balances in the design summary's table. Verify: `HeadlineAcceptanceSpec` passes and the whole `dotnet test` run is green.
- [ ] 4.6 Write `README.md` from the design summary's README contents:
  - `dotnet run --project src/Bank.Api`
  - `curl -F file=@mable_transactions.csv http://localhost:5080/transfer-batches` and `curl http://localhost:5080/accounts`
  - the expected closing Balances table, and `dotnet test`
  - the .NET 10 note, and "AUD assumed"
  - the known limitations, plus the `dotnet clean` reset and `PreserveNewest` note
  - Verify: every command in it runs as written from a fresh clone.
- [ ] 4.7 Manual check of the scenarios that can't run in-process:
  - `dotnet run --project src/Bank.Api` from the repo root starts on 5080 with no prompts, and `curl` against `/accounts` gives 200 with no redirect;
  - after an upload, stopping and restarting the service keeps $4,820.50;
  - `dotnet clean` then running again gives $5,000.00;
  - `git status` shows `mable_account_balances.csv` unchanged.
  - Verify: each check behaves as described.
