# Domain Glossary

Shared vocabulary for the Expense Tracker. Use these terms exactly — in code, commits, PR titles, and conversations with the team — so we don't drift into synonyms ("entry", "purchase", "record") that blur meaning.

## Expense

A single outflow of money the user wants to track. The only aggregate in the system today.

An Expense is **immutable once created**: there is no edit or delete endpoint. If the user records something wrong, the current expectation is that they live with it or we add a corrective entry later. Treat this as a deliberate constraint, not a missing feature — re-confirm with the product owner before adding mutation endpoints.

### Fields

| Field         | Type             | Meaning                                                                                                  |
| ------------- | ---------------- | -------------------------------------------------------------------------------------------------------- |
| `Id`          | `Guid`           | Stable identifier. Generated client-side on the domain object, never reused.                             |
| `Amount`      | `decimal(18, 2)` | How much was spent, in the user's implicit currency (see *Currency*). Always **positive**.               |
| `Description` | `string`         | Free-text label, 1–200 chars, trimmed. Required.                                                         |
| `Date`        | `DateOnly`       | The **spent-on** date — when the money left the user's pocket. Not when the row was entered.             |
| `CreatedAt`   | `DateTimeOffset` | When the row was inserted, in UTC. Set by Postgres (`now() at time zone 'utc'`), never assigned in C#.   |

### `Date` vs. `CreatedAt` — don't confuse them

This is the single most important distinction in the domain:

- **`Date`** answers *"when did I spend this?"* — it's user-supplied, a calendar date with no time or zone, and it's what the user sorts and filters by.
- **`CreatedAt`** answers *"when did the system learn about this?"* — it's the audit timestamp, server-assigned, and used only as a tiebreaker when two expenses share the same `Date`.

If you find yourself reaching for `CreatedAt` in a user-facing list or filter, you almost certainly want `Date`.

## Amount

A positive `decimal` with exactly **2 decimal places** (`HasPrecision(18, 2)`). Inputs are rounded half-away-from-zero on create — `12.345` becomes `12.35`, `-0` is rejected as `<= 0`.

There is no concept of debit/credit or sign: every Expense is a spend. Refunds, reimbursements, and income are **out of scope** in the current model. If we add them, they'll be a separate aggregate (e.g. `Income`) or an `ExpenseKind` discriminator — not a negative `Amount`.

## Currency

Currently **implicit and single-currency**. The schema has no currency column; the app assumes the user tracks expenses in one currency and is responsible for not mixing. When a multi-currency feature is needed, treat it as a schema change (add `Currency` as ISO 4217 on `Expense`) rather than overloading `Description`.

## Description

Free-text, 1–200 characters. Trimmed on create (leading/trailing whitespace removed). Whitespace-only descriptions are rejected as missing.

We do **not** parse descriptions for categories, tags, or merchants — they're opaque strings. Don't add regex-based categorization without a design discussion; the right move is a real `Category` field.

## Date semantics on the wire

`Date` is serialized as an ISO `YYYY-MM-DD` string in both request and response JSON. It is **not** a JS `Date` object, not a timestamp, and carries no time-of-day or time zone information. Two users in different time zones recording an expense for "today" will produce different `Date` values — that's intentional; the date is the user's local calendar date, captured client-side.

`CreatedAt` is serialized as an ISO-8601 timestamp in UTC.

## Sort order

Lists are sorted by `Date` descending, then `CreatedAt` descending. The intent: today's expenses on top; within a single day, most-recently-entered first. Don't change this without checking the UI — the SPA assumes this order and does not re-sort.

## Out of scope (today)

These concepts come up in conversation but **do not exist in the model**. If you see them in code, treat it as a bug or a stale rename:

- **Category / Tag / Merchant** — no taxonomy yet; everything lives in `Description`.
- **User / Account** — the app is single-tenant; there is no auth and no `UserId` on `Expense`.
- **Budget** — no spending limits, alerts, or periods.
- **Recurring expense** — every Expense is a one-off row.
- **Attachment / Receipt** — no file storage.
- **Soft delete / archive** — rows are permanent; see *Expense* above.
