# Loot V2 — Product Requirements Document

## Overview

Loot V2 is a personal finance desktop application built on WinUI 3 (.NET 8). It lets the user maintain a monthly budget of recurring expenses, track a running transaction list, import OFX bank files, and reconcile real transactions against expected ones.

---

## Pages & Navigation

| Nav Item | Page | Purpose |
|---|---|---|
| Transactions | TransactionsPage (rename MainPage) | Monthly transaction ledger |
| Imports | ImportsPage (strip Plaid UI) | OFX file loading and transaction browser |
| Budget | BudgetPage | Recurring expense template CRUD |
| Settings | SettingsPage | Theme, app info (unchanged) |

---

## 1. Budget Page

### Purpose
Manage a persistent list of recurring monthly expenses that serve as the template for each new month.

### Data Model (`Loot V2.Core`)
```
BudgetItem
  - Name        : string
  - Amount      : decimal
  - DayOfMonth  : int (1–31)
```

### UI
- `DataGrid` listing all budget items (Name, Amount, Day of Month columns)
- Add / Edit / Delete actions (toolbar buttons or inline editing)
- Monthly total displayed at the bottom (sum of all `Amount` values)

### Persistence
- Serialized as XML to `%LocalAppData%\Loot_V2\budget.xml`
- Loaded on app startup; saved automatically when changes are made to the budget

---

## 2. Transactions Page

### Purpose
The primary working view for a single month — shows expected expenses, reconciled actuals, and unexpected bank transactions in one sorted ledger.

### Columns
| Column | Description |
|---|---|
| Date | Expected day-of-month, or actual OFX date once reconciled |
| Name | Budget item name, or OFX merchant name once reconciled |
| Amount | Expected amount, or actual OFX amount once reconciled |
| Status | Expected / Reconciled / Unexpected |
| Running Balance | Cumulative balance from Starting Balance, top-to-bottom |

### Row States & Colors
| Status | Color |
|---|---|
| Expected | Default (no highlight) |
| Reconciled | Green |
| Unexpected | Amber |

### Sort Order
- Primary: Date (day-of-month for Expected rows; actual OFX date for Reconciled/Unexpected)
- The "Starting Balance" row is always pinned to the top

### Running Balance Calculation
Starting Balance − debits + credits, accumulated top-to-bottom through the sorted list.

### Starting Balance Row
- Created automatically when a new month is started, initialized to $0.00
- Auto-updated to the OFX file's `<LEDGERBAL>` value when an OFX file is loaded on the Imports page

### Toolbar
- **New Month** button — opens the Budget Item Picker dialog
- **Save** button — saves the current month XML file (or triggers Save As if not yet saved)
- **Open** button — opens an existing month XML file (prompts to save first if dirty)
- **Save As** button — saves to a user-chosen file path

### Title Bar
Displays current filename and dirty state:
```
Loot V2 — 2026-05.xml *
```
The asterisk appears whenever there are unsaved changes.

### New Month Flow
1. User clicks **New Month**
2. If current month has unsaved changes, prompt: "Save changes before continuing?"
3. **Budget Item Picker** modal dialog opens:
   - Lists all budget items (Name, Amount, Day of Month) with checkboxes
   - **Add All** button to select all items at once
   - **OK** / **Cancel** buttons
4. On OK: creates a new blank month dataset containing:
   - A "Starting Balance" row ($0.00, pinned top)
   - One Expected row per selected budget item

### Reconciliation Flow (Right-Click on Expected Row)
1. User right-clicks an Expected row → context menu appears with **"Import"** option
2. **Match Dialog** (modal `ContentDialog`) opens:
   - Title shows the expected expense being matched
   - Lists all **unmatched** OFX transactions from the currently loaded OFX file
   - Best candidate is highlighted at the top (scored by amount + name + date — see Auto-Match below)
   - User selects a row and clicks **Match** (or double-clicks)
3. On confirmation:
   - The Expected row is replaced with OFX transaction data, status → **Reconciled** (green)
   - The matched OFX transaction is marked as matched on the Imports page

### Auto-Match Scoring
Candidates are ranked by a composite score (higher = better match):

| Signal | Weight | Criterion |
|---|---|---|
| Amount | High | Exact match = full points; within 5% = partial |
| Name | Medium | OFX transaction name contains keywords from budget item name (case-insensitive) |
| Date | Medium | OFX transaction date within ±5 days of expected day-of-month |

### Save / Open Behavior
- **Save**: writes month XML to previously chosen path (or triggers Save As if none)
- **Save As**: standard Windows file-save dialog, `.xml` filter
- **Open**: standard Windows file-open dialog, `.xml` filter; prompts to save first if dirty
- **New Month**: prompts to save first if dirty

---

## 3. Imports Page

### Purpose
Load an OFX file downloaded from the bank's website, browse all bank transactions, and either match them to expected expenses (via the Transactions page) or add unexpected ones directly to the month.

> **Note:** All Plaid UI (Connect Bank Account button, Refresh button) is removed. The `IPlaidService` / `PlaidService` and related code are retained in the codebase for potential future use but are not registered or exposed in the UI.

### Toolbar
- **Load OFX File** button — opens a file-open dialog (`.ofx`, `.qfx` filter), parses the file using the existing **OFXSharp** library, and populates the transaction list
- When loaded: updates the Transactions page's Starting Balance from the OFX `<LEDGERBAL>` value

### Transaction List Columns
| Column | Description |
|---|---|
| Date | OFX transaction date |
| Name | OFX merchant / transaction name |
| Amount | OFX amount |
| Status | Unmatched / Matched |

### Row States
- **Unmatched**: default color, available for reconciliation
- **Matched**: visually distinct (e.g., greyed out) — already linked to a Transactions row

### Right-Click Context Menu (on OFX row)
- **Add to Transactions** — adds the OFX transaction to the Transactions page as an **Unexpected** (amber) row; row is then marked Matched on the Imports page

### Monthly XML Persistence
- The loaded OFX transaction data is included in the monthly XML file (so re-opening a saved month restores the Imports page state)
- The matched/unmatched status of each OFX transaction is also persisted

---

## 4. Monthly XML File Format

The monthly file captures the full state of one month's ledger. Budget template data is **not** included.

```xml
<Month year="2026" month="5" file-path="C:\...">
  <StartingBalance amount="1234.56" />
  <Transactions>
    <Transaction
      date="2026-05-01"
      name="Rent"
      amount="1500.00"
      status="Reconciled"
      ofxId="TXN20260501001"
    />
    <Transaction
      date="2026-05-15"
      name="Car Payment"
      amount="387.00"
      status="Expected"
    />
    <Transaction
      date="2026-05-03"
      name="Amazon"
      amount="42.99"
      status="Unexpected"
      ofxId="TXN20260503005"
    />
  </Transactions>
  <OFXImport loaded="true">
    <OFXTransaction
      id="TXN20260501001"
      date="2026-05-01"
      name="RENT PAYMENT"
      amount="1500.00"
      matched="true"
    />
    <OFXTransaction
      id="TXN20260503005"
      date="2026-05-03"
      name="AMAZON MKTPLACE"
      amount="42.99"
      matched="true"
    />
    <!-- ... -->
  </OFXImport>
</Month>
```

---

## 5. Data Models (`Loot V2.Core`)

```
BudgetItem
  - Id          : Guid
  - Name        : string
  - Amount      : decimal
  - DayOfMonth  : int

MonthTransaction
  - Id          : Guid
  - Date        : DateOnly
  - Name        : string
  - Amount      : decimal
  - Status      : TransactionStatus (Expected | Reconciled | Unexpected)
  - OFXTransactionId : string?   // null for unreconciled Expected rows

OFXTransaction
  - Id          : string         // OFX FITID
  - Date        : DateOnly
  - Name        : string
  - Amount      : decimal
  - IsMatched   : bool

MonthData
  - Year        : int
  - Month       : int
  - StartingBalance : decimal
  - Transactions    : List<MonthTransaction>
  - OFXTransactions : List<OFXTransaction>

TransactionStatus (enum)
  - Expected
  - Reconciled
  - Unexpected
```

---

## 6. Services (`Loot V2.Core`)

| Service | Responsibility |
|---|---|
| `IBudgetService` | Load/save `budget.xml`; CRUD for `BudgetItem` list |
| `IMonthDataService` | Load/save monthly XML; manage `MonthData` |
| `IOFXImportService` | Parse OFX file via OFXSharp; extract transactions and ledger balance |
| `IMatchingService` | Score OFX transactions against a `MonthTransaction`; return ranked candidates |

---

## 7. Architecture Notes

- **`Loot V2.Core`**: All domain models, service interfaces, and implementations that do not touch WinUI (budget, month data, OFX parsing, matching logic)
- **`Loot V2`**: All WinUI pages, ViewModels, DI wiring, and file-dialog interactions
- **OFXSharp**: Already present in the solution — use as-is for OFX parsing
- **Plaid**: `IPlaidService`, `PlaidService`, `PlaidLinkWindow`, `PlaidTransaction` — all retained in codebase, none registered in DI or referenced from UI
- **DI Registration**: New services registered as singletons in `App.xaml.cs`; new ViewModels registered as transient

---

## 8. Out of Scope (v1)

- Multi-account support
- Charts or spending analytics
- Export (CSV, PDF)
- Categories on transactions
- Cloud sync or backup
- Plaid live bank connection (code retained for future use)
