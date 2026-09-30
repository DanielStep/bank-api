# Design Interview Log

Record of the design interview for the Mable back end code challenge, held on 30/09/2026. It captures each question, the options put forward, the recommendation and the final decision, plus the research findings behind them.

Related files:
- [Design summary](./design-summary.md): the agreed design, ready to hand to later steps.
- [CONTEXT.md](../CONTEXT.md): the glossary of domain terms.
- [ADR 0001](../adr/0001-gross-multi-pass-settlement.md): gross, multi-pass settlement.
- [ADR 0002](../adr/0002-xunit-and-shouldly-in-place-of-rspec.md): xUnit v3 and Shouldly in place of RSpec.

## Framing

- **What the reviewer requires** is only the top of [the brief](../MABLE_BACK_END_CODE_TEST.md): the scenario, the two sample CSVs and the rubric. Everything from "Detailed Specification:" downward is the author's own addition and is **not** a reviewer requirement. That includes FastEndpoints, NSpec, CQRS, the validator and the HTTP API.
- **Language:** the author has been told they may use any language they're comfortable with, and has chosen .NET 10.
- **Rubric lines that carry the most weight:** "uses rspec" (replaced by a .NET BDD framework, see ADR 0002), "runs and provides feedback" and "calculates test files accurately".

## Research findings (checked 30/09/2026)

### Local environment
- The machine has the .NET 10.0.11 **runtime** only, with **no SDK installed**, so nothing can build yet. Fix: `sudo pacman -S dotnet-sdk`.
- The probes below used SDK 10.0.401, installed temporarily in the session scratchpad.

### NSpec (the original test-framework choice)
- `NSpec` 3.1.0 was released on 12/06/2017 and targets netstandard1.6 and net451. `NSpec.VsAdapter` 1.0.0 dates from 2017.
- The GitHub repo's last push was in 2022. Treat it as unmaintained.
- On net10.0, `dotnet test` with the VsAdapter reports "No test is available".
- NSpec only works through a custom console `Main` or a single xUnit test that runs the whole suite.

### Other BDD frameworks
- **Reqnroll 3.3.4** (23/03/2026), BSD-3: active; it's the successor to SpecFlow, which reached end of life on 31/12/2024.
- **LightBDD 3.12.1:** incompatible with xunit.v3 4.x. `dotnet test` reported a pass while silently dropping the scenarios.
- **Machine.Specifications 1.1.3:** maintained, but slowly.
- **xunit.v3 4.0.1** (12/09/2026), Apache-2.0: very active.
  - It needs `global.json` → `{"test":{"runner":"Microsoft.Testing.Platform"}}` on the .NET 10 SDK.
  - Drop `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`.
  - Nested classes are discovered and shown as `Outer+Inner.method`.
- **NUnit 5.0.0** was released on 27/09/2026.

### Assertion libraries
- **Shouldly 4.3.0**, BSD-3.
- **FluentAssertions 8.x** uses the Xceed licence: free only for non-commercial use.
- **AwesomeAssertions**, Apache-2.0: the community fork of FluentAssertions.

### FastEndpoints
- 8.3.0 (20/08/2026) targets net8, net9 and net10, and its command bus, `IFormFile` upload and `Validator<T>` all work.
- A raw `text/csv` body returns 415 unless it's declared with `Accepts(...)`.
- FastEndpoints.Testing 8.3.0 supports only xunit v3 (4.x or later).

### MediatR
- **Licence:** commercial from 13.0.0 onwards. The latest is 14.2.0; the last Apache-2.0 release is 12.5.0 (01/04/2025).
- **Community tier:** revenue must be under US$5M. Government and quasi-government agencies are excluded, and it can't be used to deliver services for organisations that aren't eligible themselves.
- **No licence key:** MediatR only logs a warning.
- *This is a sub-agent's reading of the licence and needs qualified legal review before anyone relies on it.*

### Mediator (martinothamar)
- 3.0.2 is MIT, source-generated, and has the same request/handler shape as MediatR.

### Minimal API
- An `IFormFile` endpoint returns HTTP 500 (antiforgery middleware missing) until `.DisableAntiforgery()` is added.
- `WebApplicationFactory` (Microsoft.AspNetCore.Mvc.Testing 10.0.12) works with xunit.v3 4.x.
- .NET 10 no longer needs `public partial class Program {}`.

### Zero-config `dotnet run` on Windows and Mac
- Use HTTP only (avoids the dev-cert prompt).
- Avoid port 5000 (macOS AirPlay uses it).
- Build paths from the content root or base directory with `Path.Combine`.
- Handle CRLF line endings in the CSVs.

## Decisions

"Recommended" means the recommendation was accepted as proposed. Where the user changed or overrode something, their words are quoted or summarised.

### Round 1

| # | Question | Decision |
|---|---|---|
| Q1 | How the CSV is sent | Recommended: **multipart/form-data file upload** (`IFormFile`). |
| Q2 | Transfer vs Transaction | User: **"use transfer"**. "Transaction" is avoided. |
| Q3 | How the multi-pass return queue works | Recommended; details below. |
| Q4 | Reject the whole file or one Transfer? | Recommended: **malformed CSV → 400 for the whole request**, nothing applied. **Business-rule breach → only that Transfer is Rejected** with a reason, and the rest settles. |
| Q5 | Writing balances to disk | Recommended at first (working copy, seed file never changed), later **superseded by Q17 and Q17b**. |
| Q6 | Shape of the domain model | Recommended; details below. |
| Q7 | Value objects | Recommended; details below. |
| Q8 | CSV header row | User: **match the samples**. No header row is expected. |
| Q9 | Company in the model | Recommended: **no Company entity or route segment**. One Company per deployment. |
| Q10 | Concurrency and duplicate uploads | Recommended: **one `SemaphoreSlim`** runs one settlement at a time. **No idempotency**: re-posting a file settles it again. Both go in the README as known limitations. |
| Q11 | Endpoints | User: **just the two endpoints**, `POST /transfer-batches` and `GET /accounts`. No balance-upload endpoint. |
| Q12 | Solution layout | User rejected the combined Api project: *"I want proper separation."* Details below. |
| Q13 | NSpec setup | User: *"NSpec is my assessment of alternative to RSpec, something else can be used as long as it's standard well supported BDD."* This led to Q22. |

**Q3 details: multi-pass settlement**
- Pass 1 walks the Transfers in file order, and a Transfer the sender can't afford is Deferred.
- Later passes retry the Deferred Transfers in their original order.
- Settlement stops when a pass settles nothing, and anything still Deferred is Rejected.
- Agreed edge cases:
  - A **cycle** between unfunded Accounts is Rejected (gross settlement, no netting).
  - **File order sets priority.**
  - A **later Transfer may overtake** an earlier Deferred one from the same Account.

**Q6 details: domain model**
- `Account` entity with debit and credit operations that guard the no-overdraft rule.
- `TransferBatch` owns the pass, defer and reject logic plus the batch-level rules (accounts exist, no transfer to yourself).
- The command only loads, maps, calls `Settle` and saves.

**Q7 details: value objects**
- `AccountNumber`: 16 digits, stored as a **string** so leading zeros survive.
- `Money`: a `decimal` with at most 2 decimal places. More than 2 is **refused as malformed (400)** and never rounded.
- Single currency with no code modelled; the README says "AUD assumed".

**Q12 details: layers**
- Four layers: **Api**, **Application** (commands, parsers), **Domain**, and **Data** (the repository).

### Round 2

| # | Question | Decision |
|---|---|---|
| Q14 | Other terms | Accepted because Q3 and Q6 adopted them: **Transfer Batch, Settlement, Settled, Deferred, Rejected**, plus Company and Balance (see CONTEXT.md). |
| Q15 | Which layer owns CSV parsing | User: *"keep it simple, just have parser do validation and return errors, no need for validator."* **No FastEndpoints validator.** `TransferCsvParser` (Application) returns rows or line-numbered errors, and on errors the command returns an error result that the endpoint maps to a 400. |
| Q16 | Where the repository interface lives | Recommended: **`IAccountRepository` in Domain.** Data implements it and never references Application. |
| Q17 | What the Data layer does | User: *"I don't want the repository doing any of that… just read the sample balances csv as the working file."* No seed copy and no write-temp-then-rename. |

### Round 3

| # | Question | Decision |
|---|---|---|
| Q17b | Using the sample balances file as the working file | Recommended: the **csproj copies `mable_account_balances.csv` into the build output** (`CopyToOutputDirectory=PreserveNewest`), and the repository reads and writes that copy. The repo's file is never touched, and `dotnet clean` resets it. |
| Q18 | Which rules Reject straight away | Recommended: `UnknownSendingAccount`, `UnknownReceivingAccount`, `SameAccount` and `NonPositiveAmount` are **Rejected on pass 1 and never retried**. Only `InsufficientFunds` Defers, and a Transfer still Deferred at the end is Rejected with `InsufficientFunds`. **`SameAccount` moved into `Transfer.Create` by Q31.** |
| Q19 | Where the positive-amount rule lives | Recommended: **`Money` is non-negative.** The parser lets any sign through, and the domain `Transfer.Create(...)` returns a Rejected Transfer (`NonPositiveAmount`) for an amount ≤ 0. The parser allows any sign because of Q4: `-5.00` is well-formed CSV that breaks a business rule, so only that Transfer is Rejected rather than the whole file returning 400. **Amended by Q27.** |
| Q20 | Response shape | Recommended; example below. |
| Q23 | Spec projects | Recommended; list below. |

**Q20 response**
- **Status:** 200 whenever the CSV is well-formed, even if every Transfer is Rejected.
- **Contents:**
  - The Settled Transfers, in the order they settled.
  - The Rejected Transfers, with reasons.
  - The closing balances.
  - Line numbers on every Transfer, and amounts as JSON numbers.
  ```json
  {
    "settled":  [{ "line": 1, "from": "...", "to": "...", "amount": 500.00 }],
    "rejected": [{ "line": 4, "from": "...", "to": "...", "amount": 25.60, "reason": "InsufficientFunds" }],
    "balances": [{ "accountNumber": "...", "balance": 4820.50 }]
  }
  ```

**Q23 spec projects**
- `Bank.Domain.Specs`: pure, no fakes.
- `Bank.Application.Specs`: the parser, plus the command with an in-memory repository fake.
- `Bank.Data.Specs`: the file repository against a temporary file.
- `Bank.Api.Specs`: in-process, end-to-end, with each spec using its own temporary data file.
- Each project specifies only its own layer.

### Round 4

| # | Question | Decision |
|---|---|---|
| Q21 | ADRs | User asked what "netting" meant; explanation below. Final: **ADR 0001, gross multi-pass settlement.** The single-Company ADR was dropped, because "single company" is in the reviewer's brief and so isn't surprising. |
| Q22 | Test framework | Recommended: **xUnit v3 4.x + Shouldly**, nested describe/context/it classes, `WebApplicationFactory` for API specs, `global.json` enabling Microsoft Testing Platform. **ADR 0002** records why RSpec is replaced. |
| Q24 | Web framework | User considered Minimal API + MediatR instead of FastEndpoints. Final (recommended): **Minimal API with no mediator library.** The endpoint injects the Application handler and calls it directly, and CQRS shows through the command and handler classes. MediatR was avoided for licence reasons, and a mediator was judged unnecessary for one command and one query. |
| Q25 | How the reviewer gets feedback | User: *"keep it simple, the reviewer won't be using a .NET IDE."* **The README gives `dotnet run` plus one `curl` line and the expected results.** No `.http` file, no OpenAPI UI, no console entry point. |
| Q26 | Language risk | User: *"I have been informed that I can use what language I'm comfortable with, I'm going with .NET."* One line in the README notes it. |

**Q21: what netting means**
- **Gross** (chosen): each Transfer settles on its own and needs the full amount in the sending Account at that moment.
- **Net**: sum each Account's incoming and outgoing Transfers across the batch, and apply everything at once if every net position stays ≥ $0.
- **Example:** A (0) → B 100 and B (0) → A 100 are both Rejected under gross settlement but both Settle under net settlement.
- **Why not net:** once any Account nets negative, choosing which Transfers to drop is a subset-selection optimisation problem with no simple, fair answer. Real systems do both: RITS settles gross in real time, and BECS direct entry settles on a deferred net basis. Confirm those details before quoting them.

### Round 5: domain model review

| # | Question | Decision |
|---|---|---|
| Q27 | How a Rejected Transfer holds a negative amount | Review found that Q19 couldn't be built: a Transfer's amount was `Money` (≥ 0), so `Transfer.Create` had no way to hold `-5.00` to reject and report it. User proposed letting `Money` be negative; that was set aside because it removes the type's guarantee that amounts and Balances are never negative, and replaces it with extra guards in `Account` (amount > 0 on withdrawals and deposits, opening Balance ≥ 0). Final: **`Money` stays ≥ 0. A Transfer keeps the requested amount as a `decimal` of any sign, owns its status, and only creates `Money` once the amount is known to be positive.** `Settle` skips Transfers already Rejected at creation. |
| Q28 | Where the overdraft rule lives | Review found the rule in two places: `TransferBatch` compared the Balance itself, and `Account` refused the withdrawal. Options: `TryWithdraw` returning a bool, `CanWithdraw` then `Withdraw`, or catching an exception. Final (recommended): **`Account.TryWithdraw(Money)` returns `false` and changes nothing when funds are short; `Transfer.SettleBetween(sender, receiver)` uses it to set Settled or Deferred.** The batch owns passes, order and lookup; `Account` owns the $0 floor; `Transfer` owns its status. |
| Q29 | Line number in the domain | Review found `Transfer` carried a CSV line number, although the brief says the CSV must not reach the domain. Final (recommended): **the domain uses `Position`, the Transfer's 1-based place in the Transfer Batch.** The command maps line → Position (always equal), and the API response keeps the name `line` for the reader of the file. **Position** added to CONTEXT.md. |
| Q30 | Who owns the set of Accounts | Review found `Settle` took a bare collection, so "each Account number once" was only a Data startup check and lookup was loop code in the batch. Options: an `Accounts` class, making Company the owner (reopening Q9), or no change. User: **A. An `Accounts` class backed by a `Dictionary<AccountNumber, Account>`**: its constructor refuses duplicates and `Find` returns the Account or null. `IAccountRepository` returns and saves `Accounts`. No glossary entry, as it's just the plural. |
| Q31 | Which reason wins when several apply | Review found no precedence, e.g. for an unknown Account sending −$5.00 to itself. Final (recommended): **one reason per Transfer, the first check that fails. `Transfer.Create` checks `NonPositiveAmount` then `SameAccount` (both need only the Transfer); `Settle` checks `UnknownSendingAccount` then `UnknownReceivingAccount`; `InsufficientFunds` comes last.** `SameAccount` moves from `Settle` into `Create`, amending Q18. |
| Q32 | Glossary gaps | Review found terms used but undefined. Final (recommended): **added Sending Account, Receiving Account, Pass and Rejection Reason to CONTEXT.md.** Domain properties are `SendingAccount` and `ReceivingAccount`; `from`/`to` stay only in the CSV parser and the JSON, as the file's words. "Cycle" is avoided for a Pass because ADR 0001 uses it for Transfers that loop (A→B→A). "For a single day" stays in Transfer Batch as the business meaning; the README's known limitations cover re-posting. |
| Q33 | What `Settle` returns | User: **a named `SettlementResult`** holding the Settled Transfers (in settle order), the Rejected Transfers with reasons, and the updated Accounts. |

### Round 6: planning the OpenSpec change

| # | Question | Decision |
|---|---|---|
| Q34 | The status of a valid Transfer before its first Pass | Review found that `TransferStatus` (Settled, Deferred, Rejected) had no state for a Transfer that has passed `Create`'s checks but not yet been tried. User: **replace Deferred with Unsettled.** A Transfer that passes `Create` starts Unsettled, stays Unsettled when its Sending Account is short, and is Rejected as `InsufficientFunds` if it is still Unsettled when Settlement stops. CONTEXT.md, the design summary and ADR 0001 were updated to match. |

### Round 7: console batch job

| # | Question | Decision |
|---|---|---|
| Q35 | HTTP API or console job | User, on re-reading the brief ("load account balances … and then accept a day's transfers"): *"This isn't an API, it's a simple console batch job that runs and has two file names passed to it as arguments."* **The Api is replaced by a console job** that takes the balances file and the Transfer Batch file, and prints the report the upload used to return plus the updated balances file. **`GET /accounts` and the get-accounts query are removed.** Supersedes Q1, Q11, Q17b, Q24's Minimal API and Q25. |
| Q36 | Writing the balances file | User: **write the closing balances back to the balances file passed in, then print it.** The build-output working copy (Q17b) is dropped, so running on the sample changes the repo's file. |
| Q37 | Follow-ups to the console job | User: *"There's no need for a lock, the test shouldn't break anything, rename bank api to bank batch job."* **The settlement `lock` is removed** (one run per process). **The headline spec settles the repo's `mable_transactions.csv` against the brief's opening balances** instead of reading `mable_account_balances.csv`, so a run of the job can't break it. **`BankApi.slnx` becomes `BankBatchJob.slnx` and the OpenSpec capability `bank-api` becomes `bank-batch-job`.** |
| Q38 | How the job gets its dependencies | User: *"I want you to use dependency injection here"*, and chose a container over constructor injection wired by hand. **`Program.cs` registers the repository, handler and `SettlementJob` in `Microsoft.Extensions.DependencyInjection`** after checking the arguments, because the repository needs the balances path. `SettlementJob` takes the handler in its constructor. |

## Worked check: sample files

All four sample Transfers Settle on pass 1.

| Account | Opening | Closing |
|---|---:|---:|
| 1111234522226789 | 5,000.00 | 4,820.50 |
| 1111234522221234 | 10,000.00 | 9,974.40 |
| 2222123433331212 | 550.00 | 1,550.00 |
| 1212343433335665 | 1,200.00 | 1,725.60 |
| 3212343433335755 | 50,000.00 | 48,679.50 |
| **Total** | **66,750.00** | **66,750.00** |
