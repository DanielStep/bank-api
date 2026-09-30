# Banking

A simple banking service that holds one company's customer account balances and applies the day's transfers that the company submits.

## Language

### Accounts

**Company**:
The business that holds accounts for its customers and submits their transfers. The service serves exactly one Company.
_Avoid_: Client, tenant

**Account**:
A customer's holding of money, identified by a 16-digit account number. Its balance may never go below $0.
_Avoid_: Wallet, ledger entry

**Balance**:
The amount of money an Account currently holds, in whole cents.
_Avoid_: Funds, total

### Transfers

**Transfer**:
An instruction to move an amount of money from one Account to another.
_Avoid_: Transaction, payment

**Transfer Batch**:
The set of Transfers a Company submits for a single day, in the order submitted.
_Avoid_: Transaction file, daily transactions

**Settlement**:
Applying a Transfer Batch to the Accounts' balances, retrying Transfers until no further Transfer can be settled.
_Avoid_: Processing, reconciliation

**Settled**:
A Transfer whose amount has moved from its sending Account to its receiving Account.
_Avoid_: Completed, applied, succeeded

**Deferred**:
A Transfer that could not be settled yet and will be retried later in the same Settlement.
_Avoid_: Pending, queued, failed

**Rejected**:
A Transfer that Settlement has finished with without moving any money, together with the reason.
_Avoid_: Failed, declined
