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

**Sending Account**:
The Account a Transfer moves money out of.
_Avoid_: From account, source, payer

**Receiving Account**:
The Account a Transfer moves money into.
_Avoid_: To account, destination, payee

**Transfer Batch**:
The set of Transfers a Company submits for a single day, in the order submitted.
_Avoid_: Transaction file, daily transactions

**Position**:
A Transfer's place in its Transfer Batch, in the order the Company submitted it.
_Avoid_: Line number, index, sequence

**Settlement**:
Applying a Transfer Batch to the Accounts' balances, retrying Transfers until no further Transfer can be settled.
_Avoid_: Processing, reconciliation

**Pass**:
One walk through the Transfers not yet Settled or Rejected, in Position order.
_Avoid_: Iteration, round, cycle

**Settled**:
A Transfer whose amount has moved from its Sending Account to its Receiving Account.
_Avoid_: Completed, applied, succeeded

**Unsettled**:
A Transfer that is not yet Settled or Rejected. Each Pass tries it again until Settlement stops.
_Avoid_: Deferred, pending, queued, failed

**Rejected**:
A Transfer that will never move any money, together with the reason it was refused.
_Avoid_: Failed, declined

**Rejection Reason**:
Why a Transfer was Rejected: the first of NonPositiveAmount, SameAccount, UnknownSendingAccount, UnknownReceivingAccount or InsufficientFunds that applies.
_Avoid_: Error, failure code
