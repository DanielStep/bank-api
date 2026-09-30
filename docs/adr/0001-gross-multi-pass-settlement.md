# Gross, multi-pass settlement in submission order

A Transfer Batch is settled gross: each Transfer settles on its own, and only if the sending Account holds the full amount at that moment. Settlement walks the batch in submission order. A Transfer the sender can't yet afford stays Unsettled and is retried on later passes, in its original order. Settlement stops when a pass settles nothing, and anything still Unsettled is then Rejected for insufficient funds. Transfers with an unknown account, the same sending and receiving account, or a non-positive amount are Rejected on the first pass and never retried. The rest of the batch still settles.

## Considered Options

- **Net settlement** (apply the whole batch if every Account's net position stays ≥ $0): rejected. Once any Account nets negative, choosing which Transfers to drop becomes a subset-selection optimisation problem with no simple, fair answer.
- **All-or-nothing batches**: rejected. One bad line would block a Company's entire day.

## Consequences

- Outcomes depend on order. Swapping two Transfers from the same Account can change which one settles.
- A cycle between unfunded Accounts (A→B 100 and B→A 100, both starting at $0) is fully Rejected, even though netting would clear it.
- A later Transfer can settle before an earlier Unsettled Transfer from the same Account.
- Settlement always finishes: each pass either settles at least one Transfer or is the last pass, so there are at most N passes.
