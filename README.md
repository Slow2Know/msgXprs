# Shipment Report

Console app that downloads every shipment from the exercise API, validates each
one against the spec, and writes a JSON report. `report.json` in the repo root
is the output from the last run.

## How to run

With the exercise API running on `http://localhost:5080`:

1. Create `appsettings.json` in the repo root (it is gitignored):

   ```json
   {
     "ApiKey": "your-api-key",
     "BaseUrl": "http://localhost:5080/"
   }
   ```

2. From the repo root (the app reads `appsettings.json` from the working
   directory):

   ```
   dotnet run --project src/ShipmentReport.Cli
   ```

   The report is written to `report.json`. Pass a different path as the first
   argument to write elsewhere:

   ```
   dotnet run --project src/ShipmentReport.Cli -- out.json
   ```

   On an unrecoverable error (API unreachable, retries exhausted, bad settings
   file) the app prints the error and exits 1.

## Decisions

### Layout

Two projects. `ShipmentReport.Core` is a class library with no I/O: the wire
DTOs, the validated `ShipmentRecord`, the validator, and the summary.
`ShipmentReport.Cli` does the I/O: settings, HTTP, and writing the file.

Core is not as clean as it could be. The validator takes the wire DTO directly,
and a rejected result keeps the raw DTO so the report can echo it, which means
Core knows the API's shape. The stricter fix is a third project between the
two. The coupling is confined to the validator's parameter and the `Rejected`
result, so for this size I left it.

### Flow

1. Fetch pages sequentially until `total_pages` is reached. Each shipment is
   deserialised into a `ShipmentRecordDto` where every field is a nullable
   string, so a bad value never fails the whole page.
2. Validate each DTO. The result is either `Valid`, wrapping a typed
   `ShipmentRecord` with just the five fields the report needs, or `Rejected`,
   wrapping the raw DTO plus a list of everything wrong with it.
3. Build the summary from the list of results: counts per status, delayed
   shipments, weight not arrived, rejected records.
4. Serialise the summary to JSON: snake_case, indented, every status counted
   even when zero, delayed entries trimmed to `shipment_id`, `reason_code` and
   `status_time`, rejected entries carrying the full raw record and a
   `problems` list.

### Validation

Bad data is expected, so it is a result, not an exception. Every problem with
a record is reported, not just the first.

- `shipment_id`: rejected if missing or blank.
- `status`: must be exactly one of the four lowercase names.
- `status_time`: must match ISO 8601 `yyyy-MM-ddTHH:mm:ss` with optional
  fraction (up to seven digits) and optional `Z` or offset. I used
  `DateTimeOffset.TryParseExact` with two explicit formats rather than
  `TryParse`, since it is too lenient, or the `"o"` format, since it is too
  strict. A timestamp with no offset is accepted and read as UTC, matching the
  spec's "ISO 8601 UTC"; a stricter reading would reject it.
- `total_weight`: parsed as `decimal` under the invariant culture, allowing
  only a decimal point and a leading sign. Negative values are rejected; it
  may be guarded elsewhere, but the check is cheap. Thousands separators are
  rejected too: the schema says the string holds a number, and accepting
  `"1,000"` would also silently read a European `"18,5"` as 185.
- `reason`: must be present when status is `delayed` and null otherwise. A
  non-delayed shipment that carries a reason is rejected, since one of the
  two fields is wrong and I can't tell which.

Parsing is culture-invariant throughout so the result does not depend on the
machine's regional settings.

### Retries

Each page is requested up to 5 times. A 429 or any 5xx response is treated as
transient and retried; any other failure stops immediately. Between attempts
the app waits for the server's `Retry-After` value if one was sent, otherwise
1, 2, 4 and 8 seconds.

Two deliberate non-retries: a connection failure means the API isn't running,
so the app reports that and exits rather than waiting 15 seconds to say the
same thing; and pages are fetched one at a time rather than in parallel, since
the API rate-limits and parallel requests would mostly be retrying each other.

## With more time

- Replace the hand-rolled retry with `Microsoft.Extensions.Http.Resilience`
  (Polly) for jitter, a maximum delay, and the date form of `Retry-After`,
  which the current code ignores.
- `Microsoft.Extensions.Configuration` with `dotnet user-secrets` for the API
  key instead of a hand-rolled settings loader.
- Keep the raw `status_time` string alongside the parsed value so a reader
  can tell whether the offset was stated or assumed.
