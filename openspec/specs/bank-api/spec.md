# bank-api Specification

## Purpose

A banking service for one Company. It holds the Company's Account Balances and settles each day's Transfer Batch, uploaded over HTTP as CSV, gross and in Position order, so that no Account ever goes below $0. It runs with a single `dotnet run` and nothing to configure.

## Requirements

### Requirement: A Transfer settles only when its Sending Account holds the full amount
A Transfer SHALL be Settled only if its Sending Account's Balance covers the full amount when the Transfer is tried. Settling moves exactly that amount from the Sending Account to the Receiving Account. No Account's Balance SHALL ever go below $0.

#### Scenario: Sending Account holds enough
```gherkin
Scenario: Sending Account holds enough
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |  500.00 |
    | 2222222222222222 |  100.00 |
  And a Transfer Batch with one Transfer of $120.50 from 1111111111111111 to 2222222222222222
  When the Transfer Batch is settled
  Then the Transfer is Settled
  And the Balance of 1111111111111111 is $379.50
  And the Balance of 2222222222222222 is $220.50
```

#### Scenario: Sending Account is emptied exactly
```gherkin
Scenario: Sending Account is emptied exactly
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |  100.00 |
    | 2222222222222222 |    0.00 |
  And a Transfer Batch with one Transfer of $100.00 from 1111111111111111 to 2222222222222222
  When the Transfer Batch is settled
  Then the Transfer is Settled
  And the Balance of 1111111111111111 is $0.00
  And the Balance of 2222222222222222 is $100.00
```

#### Scenario: Sending Account is one cent short
```gherkin
Scenario: Sending Account is one cent short
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |   99.99 |
    | 2222222222222222 |    0.00 |
  And a Transfer Batch with one Transfer of $100.00 from 1111111111111111 to 2222222222222222
  When the Transfer Batch is settled
  Then the Transfer is Rejected with Rejection Reason InsufficientFunds
  And the Balance of 1111111111111111 is $99.99
  And the Balance of 2222222222222222 is $0.00
```

### Requirement: Pass 1 walks the Transfer Batch in Position order
The first Pass SHALL try every Transfer in Position order. Each Transfer is tried against the Balances left by the Transfers before it.

#### Scenario: An earlier Transfer funds a later one
```gherkin
Scenario: An earlier Transfer funds a later one
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |  100.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 2222222222222222 | 3333333333333333  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfers are Settled in the order: Position 1, Position 2
  And the Balance of 3333333333333333 is $100.00
```

#### Scenario: The earlier of two competing Transfers wins
```gherkin
Scenario: The earlier of two competing Transfers wins
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |  100.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 1111111111111111 | 3333333333333333  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfer at Position 1 is Settled
  And the Transfer at Position 2 is Rejected with Rejection Reason InsufficientFunds
  And the Balance of 2222222222222222 is $100.00
  And the Balance of 3333333333333333 is $0.00
```

### Requirement: A Transfer the Sending Account cannot yet afford stays Unsettled and is retried
A Transfer that breaks no rule of its own starts Unsettled. When its Sending Account is short, it SHALL stay Unsettled rather than being Rejected. Each later Pass SHALL retry the Unsettled Transfers in Position order. A later Transfer MAY settle before an earlier Unsettled Transfer from the same Sending Account.

#### Scenario: Sending Account is short but receives funds later in the batch
```gherkin
Scenario: Sending Account is short but receives funds later in the batch
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |  100.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 3333333333333333  | 100.00 |
    | 2        | 2222222222222222 | 1111111111111111  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfers are Settled in the order: Position 2, Position 1
  And the Balance of 1111111111111111 is $0.00
  And the Balance of 3333333333333333 is $100.00
```

#### Scenario: A later Transfer overtakes an earlier Unsettled one from the same Account
```gherkin
Scenario: A later Transfer overtakes an earlier Unsettled one from the same Account
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |   50.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 1111111111111111 | 3333333333333333  |  50.00 |
  When the Transfer Batch is settled
  Then the Transfer at Position 2 is Settled
  And the Transfer at Position 1 is Rejected with Rejection Reason InsufficientFunds
  And the Balance of 1111111111111111 is $0.00
```

#### Scenario: Unsettled Transfers are retried in Position order
```gherkin
Scenario: Unsettled Transfers are retried in Position order
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |  100.00 |
    | 4444444444444444 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 4444444444444444  | 100.00 |
    | 2        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 3        | 3333333333333333 | 1111111111111111  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfers are Settled in the order: Position 3, Position 1
  And the Transfer at Position 2 is Rejected with Rejection Reason InsufficientFunds
```

### Requirement: Settlement stops when a Pass settles nothing
Settlement SHALL stop after the first Pass that settles no Transfer. Every Transfer still Unsettled then SHALL be Rejected with Rejection Reason InsufficientFunds and SHALL move no money. Settlement SHALL always finish.

#### Scenario: A shortfall has knock-on effects
```gherkin
Scenario: A shortfall has knock-on effects
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 2222222222222222 | 3333333333333333  | 100.00 |
  When the Transfer Batch is settled
  Then both Transfers are Rejected with Rejection Reason InsufficientFunds
  And every Balance is unchanged
```

#### Scenario: A cycle between unfunded Accounts is Rejected
```gherkin
Scenario: A cycle between unfunded Accounts is Rejected
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 2222222222222222 | 1111111111111111  | 100.00 |
  When the Transfer Batch is settled
  Then both Transfers are Rejected with Rejection Reason InsufficientFunds
  And every Balance is unchanged
```

#### Scenario: A Pass that settles something is followed by another Pass
```gherkin
Scenario: A Pass that settles something is followed by another Pass
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |    0.00 |
    | 3333333333333333 |    0.00 |
    | 4444444444444444 |  100.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 2222222222222222  | 100.00 |
    | 2        | 2222222222222222 | 3333333333333333  | 100.00 |
    | 3        | 3333333333333333 | 1111111111111111  |  50.00 |
    | 4        | 4444444444444444 | 1111111111111111  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfers are Settled in the order: Position 4, Position 1, Position 2, Position 3
  And the Balance of 1111111111111111 is $50.00
  And the Balance of 3333333333333333 is $50.00
```

### Requirement: Transfers that break a rule of their own or name an unknown Account are Rejected on Pass 1
A Transfer SHALL be Rejected on Pass 1, and never retried, when its requested amount is $0.00 or less (NonPositiveAmount), when its Sending Account and Receiving Account are the same (SameAccount), when its Sending Account is not one of the Company's Accounts (UnknownSendingAccount), or when its Receiving Account is not one of the Company's Accounts (UnknownReceivingAccount). A Rejected Transfer SHALL move no money, and the rest of the Transfer Batch SHALL still settle.

#### Scenario Outline: A Transfer breaks a rule
```gherkin
Scenario Outline: A Transfer breaks a rule
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |  500.00 |
    | 2222222222222222 |  500.00 |
  And a Transfer Batch:
    | position | sending account | receiving account | amount   |
    | 1        | <sending>       | <receiving>       | <amount> |
    | 2        | 1111111111111111 | 2222222222222222 |   100.00 |
  When the Transfer Batch is settled
  Then the Transfer at Position 1 is Rejected with Rejection Reason <reason>
  And the Transfer at Position 2 is Settled
  And the Balance of 1111111111111111 is $400.00
  And the Balance of 2222222222222222 is $600.00

  Examples:
    | sending          | receiving        | amount | reason                  |
    | 1111111111111111 | 2222222222222222 |   0.00 | NonPositiveAmount       |
    | 1111111111111111 | 2222222222222222 |  -5.00 | NonPositiveAmount       |
    | 1111111111111111 | 1111111111111111 |  10.00 | SameAccount             |
    | 9999999999999999 | 2222222222222222 |  10.00 | UnknownSendingAccount   |
    | 1111111111111111 | 9999999999999999 |  10.00 | UnknownReceivingAccount |
```

#### Scenario: A Transfer to an unknown Account is not retried after the Sending Account is funded
```gherkin
Scenario: A Transfer to an unknown Account is not retried after the Sending Account is funded
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |  100.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 9999999999999999  |  50.00 |
    | 2        | 2222222222222222 | 1111111111111111  | 100.00 |
  When the Transfer Batch is settled
  Then the Transfer at Position 1 is Rejected with Rejection Reason UnknownReceivingAccount
  And the Balance of 1111111111111111 is $100.00
```

### Requirement: A Rejected Transfer reports only the first Rejection Reason that applies
When more than one Rejection Reason applies, a Transfer SHALL be Rejected with the first of them in this order: NonPositiveAmount, SameAccount, UnknownSendingAccount, UnknownReceivingAccount, InsufficientFunds.

#### Scenario Outline: Several Rejection Reasons apply
```gherkin
Scenario Outline: Several Rejection Reasons apply
  Given the Company's only Account is 1111111111111111 with a Balance of $0.00
  And a Transfer Batch with one Transfer of <amount> from <sending> to <receiving>
  When the Transfer Batch is settled
  Then the Transfer is Rejected with Rejection Reason <reason>

  Examples:
    | sending          | receiving        | amount  | reason                  |
    | 9999999999999999 | 9999999999999999 |  -$5.00 | NonPositiveAmount       |
    | 9999999999999999 | 9999999999999999 |  $10.00 | SameAccount             |
    | 9999999999999999 | 8888888888888888 |  $10.00 | UnknownSendingAccount   |
    | 1111111111111111 | 8888888888888888 |  $10.00 | UnknownReceivingAccount |
```

### Requirement: Settlement reports every Transfer's outcome and the closing Balances
Settlement SHALL report:
- the Settled Transfers, in the order they settled;
- the Rejected Transfers, in Position order, each with its Rejection Reason and its amount exactly as requested (including a zero or negative amount);
- the closing Balance of every one of the Company's Accounts.

Each reported Transfer SHALL carry its Position, Sending Account, Receiving Account and amount.

#### Scenario: Outcomes are reported in their documented order
```gherkin
Scenario: Outcomes are reported in their documented order
  Given these Accounts:
    | account          | balance |
    | 1111111111111111 |    0.00 |
    | 2222222222222222 |  100.00 |
    | 3333333333333333 |    0.00 |
  And a Transfer Batch:
    | position | sending account  | receiving account | amount |
    | 1        | 1111111111111111 | 3333333333333333  | 100.00 |
    | 2        | 3333333333333333 | 1111111111111111  | 500.00 |
    | 3        | 2222222222222222 | 1111111111111111  | 100.00 |
    | 4        | 2222222222222222 | 2222222222222222  |  -7.25 |
  When the Transfer Batch is settled
  Then the Settled Transfers are reported at Positions 3, 1 in that order
  And the Rejected Transfers are reported at Positions 2, 4 in that order
  And the Transfer at Position 2 is reported with Rejection Reason InsufficientFunds
  And the Transfer at Position 4 is reported with amount -$7.25 and Rejection Reason NonPositiveAmount
  And the closing Balances of all three Accounts are reported
```

### Requirement: A Transfer Batch is submitted as a CSV file upload
The service SHALL accept a Transfer Batch at `POST /transfer-batches` as a `multipart/form-data` request holding one CSV file. A request with no file SHALL be answered with 400 and SHALL change no Balance.

#### Scenario: The sample Transfer Batch is uploaded
```gherkin
Scenario: The sample Transfer Batch is uploaded
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  When the Company uploads mable_transactions.csv to POST /transfer-batches
  Then the response status is 200
```

#### Scenario: The request holds no file
```gherkin
Scenario: The request holds no file
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  When the Company sends POST /transfer-batches with no file
  Then the response status is 400
  And every Balance is unchanged
```

### Requirement: The CSV holds one Transfer per line with no header row
Each line of the CSV SHALL hold exactly three comma-separated fields, in this order: the Sending Account number, the Receiving Account number and the amount. There is no header row. The line number of each line SHALL be the Position of its Transfer in the Transfer Batch. Lines MAY end in CRLF or LF, and the last line MAY omit its line ending. An amount MAY carry a leading sign and SHALL use a `.` as its decimal point. A well-formed amount of $0.00 or less SHALL NOT make the file malformed; that Transfer is instead Rejected with Rejection Reason NonPositiveAmount.

#### Scenario Outline: Line endings do not matter
```gherkin
Scenario Outline: Line endings do not matter
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And mable_transactions.csv rewritten with <ending> line endings, <final> a line ending on its last line
  When the Company uploads it to POST /transfer-batches
  Then the response status is 200
  And all four Transfers are reported as Settled

  Examples:
    | ending | final   |
    | LF     | with    |
    | LF     | without |
    | CRLF   | with    |
    | CRLF   | without |
```

#### Scenario: A negative amount is a Rejected Transfer, not a malformed file
```gherkin
Scenario: A negative amount is a Rejected Transfer, not a malformed file
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And a CSV file:
    """
    1111234522226789,1212343433335665,-5.00
    1111234522226789,1212343433335665,500.00
    """
  When the Company uploads it to POST /transfer-batches
  Then the response status is 200
  And line 1 is reported as Rejected with amount -5.00 and reason NonPositiveAmount
  And line 2 is reported as Settled
```

### Requirement: A malformed CSV is refused as a whole
The service SHALL answer a malformed CSV with 400 and a problem details body that lists every error found, each with its line number. A line is malformed when it does not hold exactly three fields, when either account number is not exactly 16 digits, or when its amount is not a number or has more than 2 decimal places. A blank line before the last line is malformed. A file holding no Transfers SHALL also be refused with 400. When a CSV is refused, no Transfer in it SHALL be settled and no Balance SHALL change.

#### Scenario Outline: A line is malformed
```gherkin
Scenario Outline: A line is malformed
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And a CSV file of three lines, where lines 1 and 3 are "1111234522226789,1212343433335665,500.00" and line 2 is "<line>"
  When the Company uploads it to POST /transfer-batches
  Then the response status is 400
  And the response lists an error for line 2
  And the response lists no error for lines 1 and 3
  And every Balance is unchanged

  Examples:
    | line                                              |
    | 1111234522226789,1212343433335665                 |
    | 1111234522226789,1212343433335665,500.00,extra    |
    | 111123452222678,1212343433335665,500.00           |
    | 1111234522226789,12123434333356650,500.00         |
    | 1111234522226789,121234343333566X,500.00          |
    | 1111234522226789,1212343433335665,five            |
    | 1111234522226789,1212343433335665,500.005         |
    | 1111234522226789,1212343433335665,                |
    |                                                   |
```

#### Scenario: Every malformed line is reported
```gherkin
Scenario: Every malformed line is reported
  Given a CSV file:
    """
    1111234522226789,1212343433335665,abc
    1111234522226789,1212343433335665,500.00
    1111234522226789,1212343433335665
    """
  When the Company uploads it to POST /transfer-batches
  Then the response status is 400
  And the response lists errors for lines 1 and 3
```

#### Scenario Outline: A file holds no Transfers
```gherkin
Scenario Outline: A file holds no Transfers
  Given a CSV file holding <content>
  When the Company uploads it to POST /transfer-batches
  Then the response status is 400
  And every Balance is unchanged

  Examples:
    | content              |
    | nothing              |
    | only a line ending   |
```

### Requirement: A well-formed CSV is settled and every outcome is reported
For a well-formed CSV the service SHALL settle the Transfer Batch and answer 200, even when every Transfer is Rejected. The JSON body SHALL hold:
- `settled`: the Settled Transfers in the order they settled, each with `line`, `from`, `to` and `amount`;
- `rejected`: the Rejected Transfers in Position order, each with `line`, `from`, `to`, `amount` and `reason`, where `reason` is the Rejection Reason's name;
- `balances`: every Account's closing Balance, each with `accountNumber` and `balance`.

`line` SHALL be the Transfer's Position, `from` its Sending Account and `to` its Receiving Account. Account numbers SHALL be JSON strings, and amounts and balances SHALL be JSON numbers.

#### Scenario: A mixed Transfer Batch
```gherkin
Scenario: A mixed Transfer Batch
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And a CSV file:
    """
    2222123433331212,1212343433335665,600.00
    1111234522226789,1111234522226789,10.00
    1111234522226789,2222123433331212,100.00
    """
  When the Company uploads it to POST /transfer-batches
  Then the response status is 200
  And "settled" is:
    | line | from             | to               | amount |
    | 3    | 1111234522226789 | 2222123433331212 | 100.00 |
    | 1    | 2222123433331212 | 1212343433335665 | 600.00 |
  And "rejected" is:
    | line | from             | to               | amount | reason      |
    | 2    | 1111234522226789 | 1111234522226789 |  10.00 | SameAccount |
  And "balances" holds the closing Balance of each of the five Accounts
```

#### Scenario: Every Transfer is Rejected
```gherkin
Scenario: Every Transfer is Rejected
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And a CSV file:
    """
    9999999999999999,1212343433335665,10.00
    """
  When the Company uploads it to POST /transfer-batches
  Then the response status is 200
  And "settled" is empty
  And "rejected" holds line 1 with reason UnknownSendingAccount
  And every Balance is unchanged
```

### Requirement: The sample files settle to the expected closing Balances
Uploading `mable_transactions.csv` against the opening Balances in `mable_account_balances.csv` SHALL settle all four Transfers and give the closing Balances below.

#### Scenario: Headline acceptance
```gherkin
Scenario: Headline acceptance
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  When the Company uploads mable_transactions.csv to POST /transfer-batches
  Then the response status is 200
  And "settled" lists lines 1, 2, 3 and 4 in that order
  And "rejected" is empty
  And the closing Balances are:
    | account          | balance   |
    | 1111234522226789 |  4820.50  |
    | 1111234522221234 |  9974.40  |
    | 2222123433331212 |  1550.00  |
    | 1212343433335665 |  1725.60  |
    | 3212343433335755 | 48679.50  |
```

### Requirement: Each upload is settled on its own
Every accepted upload SHALL be settled as a new Transfer Batch against the current Balances. Uploading the same file again SHALL settle it again.

#### Scenario: The same file is uploaded twice
```gherkin
Scenario: The same file is uploaded twice
  Given the Company's Accounts hold the balances in mable_account_balances.csv
  And the Company has uploaded mable_transactions.csv once
  When the Company uploads mable_transactions.csv again
  Then the response status is 200
  And the Balance of 1111234522226789 is $4,641.00
```

### Requirement: The current Balances can be read
`GET /accounts` SHALL answer 200 with a JSON array that holds every one of the Company's Accounts, each with `accountNumber` (a JSON string) and `balance` (a JSON number). Accounts SHALL be listed in the order of the balances file.

#### Scenario: Opening Balances
```gherkin
Scenario: Opening Balances
  Given the service has started from mable_account_balances.csv
  When the Company sends GET /accounts
  Then the response status is 200
  And the response is:
    | accountNumber    | balance  |
    | 1111234522226789 |  5000.00 |
    | 1111234522221234 | 10000.00 |
    | 2222123433331212 |   550.00 |
    | 1212343433335665 |  1200.00 |
    | 3212343433335755 | 50000.00 |
```

#### Scenario: An account number keeps its leading zeros
```gherkin
Scenario: An account number keeps its leading zeros
  Given a balances file holding account 0000123412341234 with a Balance of $10.00
  When the Company sends GET /accounts
  Then the response lists accountNumber "0000123412341234" with balance 10.00
```

### Requirement: Balances are kept across settlements and restarts
The Balances after a Settlement SHALL be written to the working balances file before the upload is answered. Each later Settlement, each `GET /accounts` and each restart of the service SHALL start from those Balances. The working balances file SHALL keep the format it was read in: one `account,balance` line per Account, no header row, and each Balance with exactly 2 decimal places.

#### Scenario: Balances after an upload
```gherkin
Scenario: Balances after an upload
  Given the service has started from mable_account_balances.csv
  And the Company has uploaded mable_transactions.csv
  When the Company sends GET /accounts
  Then the Balance of 1111234522226789 is $4,820.50
  And the Balance of 3212343433335755 is $48,679.50
```

#### Scenario: Balances survive a restart
```gherkin
Scenario: Balances survive a restart
  Given the Company has uploaded mable_transactions.csv
  And the service has been restarted without a clean build
  When the Company sends GET /accounts
  Then the Balance of 1111234522226789 is $4,820.50
```

#### Scenario: The working balances file keeps its format
```gherkin
Scenario: The working balances file keeps its format
  Given a working balances file holding account 1111111111111111 with a Balance of $100.00 and account 2222222222222222 with a Balance of $0.00
  And a Transfer Batch with one Transfer of $0.50 from 1111111111111111 to 2222222222222222 has been settled
  When the working balances file is read as text
  Then it is:
    """
    1111111111111111,99.50
    2222222222222222,0.50
    """
```

### Requirement: The sample balances file is the starting point and is never modified
The service SHALL start from a working copy of `mable_account_balances.csv` placed next to the built service. Only the working copy SHALL ever be written; the file in the repository SHALL stay unchanged. A clean build SHALL reset the Balances to those in the repository's file.

#### Scenario: The repository's file is untouched by a Settlement
```gherkin
Scenario: The repository's file is untouched by a Settlement
  Given the service has started from mable_account_balances.csv
  When the Company uploads mable_transactions.csv
  Then mable_account_balances.csv in the repository still holds the opening Balances
```

#### Scenario: A clean build resets the Balances
```gherkin
Scenario: A clean build resets the Balances
  Given the Company has uploaded mable_transactions.csv
  And the service has been cleaned, rebuilt and started again
  When the Company sends GET /accounts
  Then the Balance of 1111234522226789 is $5,000.00
```

### Requirement: A balances file that cannot be trusted stops the service at startup
The service SHALL refuse to start when the balances file is malformed or lists the same account number twice. A line is malformed when it does not hold exactly two fields, when its account number is not exactly 16 digits, or when its Balance is not a number, is below $0.00, or has more than 2 decimal places.

#### Scenario Outline: The balances file is malformed
```gherkin
Scenario Outline: The balances file is malformed
  Given a balances file whose second line is "<line>"
  When the service starts
  Then it stops with an error that names the balances file

  Examples:
    | line                              |
    | 1111111111111111                  |
    | 1111111111111111,10.00,extra      |
    | 111111111111111,10.00             |
    | 1111111111111111,ten              |
    | 1111111111111111,-0.01            |
    | 1111111111111111,10.001           |
```

#### Scenario: The balances file lists an account twice
```gherkin
Scenario: The balances file lists an account twice
  Given a balances file:
    """
    1111111111111111,10.00
    1111111111111111,20.00
    """
  When the service starts
  Then it stops with an error that names the duplicated account number
```

### Requirement: The service runs with no configuration
Running `dotnet run --project src/Bank.Api` from the repository root SHALL start the service on `http://localhost:5080` over plain HTTP, on Windows and Mac, with no configuration, certificate or environment variable to set. The service SHALL NOT redirect to HTTPS. It SHALL find its balances file wherever the repository is cloned and whichever directory it is started from.

#### Scenario: A fresh clone is run
```gherkin
Scenario: A fresh clone is run
  Given a fresh clone of the repository and the .NET 10 SDK
  When the reviewer runs "dotnet run --project src/Bank.Api"
  Then the service answers GET http://localhost:5080/accounts with 200
```

#### Scenario: Plain HTTP is not redirected
```gherkin
Scenario: Plain HTTP is not redirected
  Given the service is running
  When the Company sends GET http://localhost:5080/accounts
  Then the response status is 200, not a redirect
```

### Requirement: The service serves one Company
Each running service SHALL hold the Accounts of exactly one Company. No request SHALL name a Company.

#### Scenario: Requests carry no Company
```gherkin
Scenario: Requests carry no Company
  Given the service is running
  When the Company sends GET /accounts
  Then the response holds the one Company's Accounts
```

### Requirement: One Settlement runs at a time
When several Transfer Batches are uploaded at the same time, the service SHALL settle them one after another. Each SHALL start from the Balances the previous one left, and no Settlement's changes SHALL be lost.

#### Scenario: Two uploads arrive together
```gherkin
Scenario: Two uploads arrive together
  Given the service has started from mable_account_balances.csv
  When the Company uploads mable_transactions.csv twice at the same time
  Then both responses have status 200
  And the Balance of 1111234522226789 is $4,641.00
```
