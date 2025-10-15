# AutoSheetData

AutoSheetData is a Unity Editor package that turns Google Sheets (public) into strongly typed ScriptableObject collection assets.

- Per sheet:
  - Automatically detects column headers and infers column types (int, float, string) from the first non-empty sample in each column.
  - Generates a typed row class and a ScriptableObject collection.
  - Creates a dedicated folder next to the config asset for generated codes.
- Sync button fetches the latest data and repopulates collections.
- Uses Google’s public CSV endpoint (no API key, no OAuth).

---

## Requirements

- Unity 2021.3 or newer.
- Your Google Spreadsheet must be public (Anyone with the link can view).
- Sheet data is fetched via the public CSV endpoint:
  - `https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}/gviz/tq?tqx=out:csv&sheet={SHEET_NAME}`

---

## First-time Setup

- Open the config window:
  - Ared > AutoSheetData > Open Config, or
  - Create the SpreadsheetConfig asset manually.
- If no config exists, the window will create one at:
  - `Assets/AutoAssetData/SpreadsheetConfig.asset`

---

## Sheet Structure

To ensure predictable code generation and reliable parsing, structure each sheet as follows:

- Row 1: Column titles (headers)
  - Every column should have a non-empty header.
  - Headers become field names (sanitized) in the generated row class.

- Rows 2…N: Data rows
  - Each row represents one record.
  - Cells may be blank, but when non-empty they must conform to the column’s data type.

- Column data types (must be consistent per column):
  - string: any non-numeric text.
  - int: whole numbers (e.g., 0, 42, -7). No thousands separators.
  - float: decimal numbers (e.g., 3.14, -0.5, 1e-3). Decimal comma is normalized to a dot.

- Type inference:
  - AutoSheetData inspects the first non-empty cell in each column to infer its type:
    - Parses as float → column type = float
    - Else parses as int → column type = int
    - Otherwise → column type = string
  - You can override any inferred type in the config’s Columns list if needed.

- Best practices:
  - Avoid mixed types within the same column (e.g., mixing “10” and “ten”).
  - Do not use merged cells.
  - Keep data atomic (no lists inside a single cell).

Valid example ✅

| Name | Damage | Weight |
|------|--------|--------|
| Sword | 25 | 2.5 |
| Axe | 40 | 3.2 |
| Bow | 15 | 1.1 |

Invalid example (mixed types in Damage column) ❌

| Name  | Damage | Weight |
|-------|--------|--------|
| Sword | 25     | 2.5    |
| Axe   | heavy  | 3.2    |
| Bow   | 15     | 1.1    |

---

## Workflow

1) Paste Spreadsheet URL:
- Paste the full Google Sheets URL into “Spreadsheet URL” and click “Parse”.
- The Spreadsheet ID is extracted automatically.

2) Add Sheets:
- In the config Inspector, click “+ Add Sheet”.
- Enter the exact sheet tab name (case-sensitive as in Google Sheets).

3) Load Columns:
- Click “Load/Refresh Columns” per sheet to:
  - Fetch the first row (headers).
  - Auto-detect the column type from the first non-empty value in that column:
    - Float if it parses as a floating-point number
    - Int if it parses as an integer
    - String otherwise
- You can still override any column’s type via the enum.

4) Generate:
- Click “Generate Data”.
- The package will:
  - Generate a row class: `[SheetName]Row`
  - Generate a collection class: `[SheetName]Collection : DataCollection<[SheetName]Row>`
  - Write files into `<ConfigFolder>/<SheetName>/Generated`
- Unity will compile automatically.

5) Create Collections:
- Click “Create Collections” to create ScriptableObject assets for collections if they don’t exist.
- Assets are created at:
  - `<ConfigFolder>/<SheetName>/[SheetName]Collection.asset`
- Optionally, enable “Auto Create Collections” in the config so they’re created after compilation.

6) Sync:
- Click “Sync” to fetch data and populate the collection assets.
- Items are stored in the `Items` list on each collection asset.
- Get items with the `GetItems()` method.


---

## Naming and Sanitization

- Class and field names are derived from Sheet names and headers.
- Invalid C# identifier characters are replaced with `_`.
- Duplicate names are deduplicated with numeric suffixes.
- Sheet and header text casing is normalized to generate readable code:
  - Class names: PascalCase + suffix Row/Collection
  - Field names: camelCase

---

## CSV & Type Rules

- The first row of each sheet is treated as column headers.
- Types are determined in the Editor from sample data (first non-empty value per column) or can be overridden manually:
  - Int: `int.TryParse(...)`
  - Float: `float.TryParse(...)` using invariant culture; comma decimals are normalized to dot
  - String: fallback for anything else

---

## Project Structure

- Config asset (default): `Assets/AutoAssetData/SpreadsheetConfig.asset`
- Per-sheet folders (next to the config asset):
  - `<ConfigFolder>/<SheetName>/Generated` (generated C#)
  - `<ConfigFolder>/<SheetName>/` (data collection asset)

---

## Tips

- Spreadsheet must be public:
  - In Google Sheets: Share > General access > “Anyone with the link” = Viewer
- Ensure Sheet names in the config match the tab names exactly.
- If you rename a sheet, re-generate types; existing assets are not auto-moved.
- To update columns after header changes, click “Load/Refresh Columns” then “Generate Types” again.

---

## Known Limitations

- Only public spreadsheets are supported out of the box (no OAuth/API Key).
- Supported column types are `string`, `int`, and `float`.
- Special characters in sheet/header names are sanitized for code generation.

---

## Troubleshooting

- “Empty CSV” or “not found”:
  - Confirm the spreadsheet is public.
  - Confirm the Sheet Name matches the tab name exactly.
- “Types not compiled”:
  - Wait for Unity to finish compilation after “Generate Data”.
  - Ensure the generated files exist under `<ConfigFolder>/<SheetName>/Generated`.
- Data parsing looks wrong:
  - Check the inferred column types and override if needed.
  - Ensure decimals use dot or can be normalized from comma.

