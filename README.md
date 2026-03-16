# CustomerTariffSwitch

.NET 10 console application that processes customer tariff-switch requests from CSV files,
applies business validation rules, and persists decisions to JSON -- idempotently across runs.

---

## Prerequisites

| Requirement | Version |
|-------------|---------|
| .NET SDK    | 10.0+   |

No external services, no environment variables, no NuGet restore beyond the SDK.

---

## Architecture

```
CustomerTariffSwitch.sln
|
+-- CustomerTariffSwitch          (entry point / console host)
|     Program.cs                  orchestrates load -> process -> persist
|     Services/ProcessRequestService.cs   business logic + SLA calculation
|
+-- CustomerTariffSwitch.Data     (I/O layer)
|     CSV-Service.cs              reads and parses all three CSV files (parallel)
|     DecisionRepository.cs       persists decisions.json (atomic, idempotent)
|     Helper/SolutionPathHelper   locates Input Files/ relative to solution root
|
+-- CustomerTariffSwitch.Models   (domain types, no dependencies)
      Customer, Tariff, SwitchRequest, RequestDecision, enums
```

### Data flow per run

```
Input Files/*.csv
      |
      v
  CsvService.ReadKnownFiles()          parse + validate rows
      |
      v
  DecisionRepository.LoadProcessedRequestIds()   skip already-handled requests (Scenario 8)
      |
      v
  ProcessRequestService.ProcessRequests()        apply Scenarios 1-7
      |
      v
  DecisionRepository.AppendDecisions()           atomic temp-file swap -> Output/decisions.json
```

### Error handling strategy

| Layer         | Behaviour                                                              |
|---------------|------------------------------------------------------------------------|
| Missing file  | Fail fast with an actionable message naming the missing file           |
| Malformed customer/tariff row | Skip row; warn to console; referencing requests get "Unknown customer/tariff" |
| Malformed request row | Reject immediately with reason "Invalid request data"; run continues  |

---

## Input files

Place the three files in the `Input Files/` folder at solution root (already present in repo).

**customers.csv** -- semicolon-delimited, UTF-8

```
CustomerId;Name;HasUnpaidInvoice;SLA;MeterType
C001;Anna Maier;false;Standard;Smart
```

| Column | Type | Values |
|--------|------|--------|
| CustomerId | string | unique key |
| Name | string | display name |
| HasUnpaidInvoice | bool | `true` / `false` |
| SLA | enum | `Standard` / `Premium` |
| MeterType | enum | `Smart` / `Classic` |

**tariffs.csv** -- semicolon-delimited, UTF-8

```
TariffId;Name;RequiresSmartMeter;BaseMonthlyGross
T01;OekostromPlus;true;49.90
```

| Column | Type | Values |
|--------|------|--------|
| TariffId | string | unique key |
| Name | string | display name |
| RequiresSmartMeter | bool | `true` / `false` |
| BaseMonthlyGross | decimal | invariant culture (`.` separator) |

**requests.csv** -- semicolon-delimited, UTF-8

```
RequestId;CustomerId;TargetTariffId;RequestedAt
R1001;C001;T01;2025-03-29T00:15:00+01:00
```

| Column | Type | Values |
|--------|------|--------|
| RequestId | string | unique key |
| CustomerId | string | reference to customers.csv |
| TargetTariffId | string | reference to tariffs.csv |
| RequestedAt | DateTimeOffset | ISO-8601 with offset |

---

## Business rules (Scenarios)

| # | Condition | Result |
|---|-----------|--------|
| 1 | Standard SLA, no unpaid invoices, smart meter not required or already installed | Approve; SLA due = RequestedAt + 48 h (Vienna local time) |
| 2 | Premium SLA, same as above | Approve; SLA due = RequestedAt + 24 h (Vienna local time) |
| 3 | Tariff requires smart meter; customer has classic meter | Approve + follow-up "Schedule meter upgrade"; SLA due = base + 12 h |
| 4 | Customer has unpaid invoice | Reject "Unpaid invoice" |
| 5 | Unknown customer ID | Reject "Unknown customer" |
| 6 | Unknown tariff ID | Reject "Unknown tariff" |
| 7 | Malformed request row | Reject "Invalid request data" |
| 8 | Already processed in a previous run | Skip |

SLA deadlines are computed in `Europe/Vienna` local time (DST-safe):
spring-forward gaps are advanced to 03:00 CEST; fall-back ambiguity resolves to the later offset (+02:00).

---

## Output

`Output/decisions.json` -- created on first run, appended on subsequent runs.

```json
[
  {
    "RequestId": "R1001",
    "Status": 0,
    "CustomerName": "Anna Maier",
    "DueAt": "2025-03-31T01:15:00+02:00"
  },
  {
    "RequestId": "R1002",
    "Status": 1,
    "CustomerName": "Stadtcafe GmbH",
    "Reason": "Unpaid invoice"
  },
  {
    "RequestId": "R1003",
    "Status": 0,
    "CustomerName": "Max Mustermann",
    "DueAt": "2025-03-31T03:15:00+02:00",
    "FollowUpAction": "Schedule meter upgrade",
    "FollowUpDueAt": "2025-03-31T03:15:00+02:00"
  }
]
```

`Status`: `0` = Approved, `1` = Rejected.
`FollowUpAction` and `FollowUpDueAt` are omitted when null (no upgrade needed).

---

## Running the application

```bash
# from solution root
dotnet run --project CustomerTariffSwitch
```

Expected console output:

```
=== CustomerTariffSwitch ===

Loading CSV files ...
  Reading customers.csv ...
  Reading tariffs.csv ...
  Reading requests.csv ...
  => 7 customers, 3 tariffs, 6 requests loaded

Processing requests ...
------------------------------------------------------------
  [APPROVED] R1001 | Anna Maier | Due: 2025-03-31T01:15:00+02:00
  [REJECTED] R1002 | Stadtcafe GmbH | Reason: Unpaid invoice
  [APPROVED] R1003 | Jamal Idris | Due: 2025-10-28T14:30:00+01:00 | Follow-up: Schedule meter upgrade
  ...
------------------------------------------------------------
  => 4 approved, 5 rejected

  => Decisions saved to: C:\...\Output\decisions.json
```

On a **second run** (no new rows added), all requests are already recorded in `decisions.json` and the run exits early:

```
  => 6 request(s) skipped (already processed in a previous run)
  => Nothing to process -- all requests have already been handled.

  To reprocess from scratch, delete the Output/ folder and re-run.
```

### Reprocessing from scratch

The `Output/` folder is excluded from version control (`.gitignore`). To reset and process all requests again:

```bash
# Windows
rmdir /s /q Output

# Unix / Git Bash
rm -rf Output/

dotnet run --project CustomerTariffSwitch
```

---

## Running the tests

```bash
# all tests
dotnet test

# unit tests only (no file I/O)
dotnet test --filter "Category!=Integration"

# integration tests only
dotnet test --filter "Category=Integration"
```

Test projects are under `CustomerTariffSwitch.Test/`:

| Folder | Trait | Coverage |
|--------|-------|----------|
| `Unit/` | (none) | CSV parsing, SLA calculation, DST edge cases, DecisionRepository with temp file |
| `Integration/` | `Category=Integration` | Full read of real CSV files via CsvService |

---

## Project structure

```
CustomerTariffSwitch/
+-- Input Files/
|     customers.csv
|     tariffs.csv
|     requests.csv
+-- Output/                   (generated; excluded via .gitignore)
+-- CustomerTariffSwitch/     (console entry point)
+-- CustomerTariffSwitch.Data/
+-- CustomerTariffSwitch.Models/
+-- CustomerTariffSwitch.Test/
|     Unit/
|     Integration/
+-- CustomerTariffSwitch.sln
+-- README.md
```
