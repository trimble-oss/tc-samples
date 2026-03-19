# Data & Sync Sample App (MAUI)

MAUI sample aligned with **PSetSync.ConsoleApp**: four SQLite databases in **one project folder**:

| File | Role |
|------|------|
| `.storage` | Main Trimble Connect project storage |
| `.catalog` | Catalog (licenses, companies, …) |
| `PSetCatalog.storage` | PSet libraries/definitions catalog |
| `PSetProject.storage` | PSet instances for the project |

All live under `FileSystem.AppDataDirectory/TCDataSyncSample` (same layout as the console app’s per-project folder).

## Migration (unencrypted → encrypted)

If you copy **older unencrypted** DBs into that folder (matching the above names), opening via **Create or open 4 DBs** runs the SDK migrations:

- **`.storage`**: e.g. V100 → V101 (SQLCipher), then onward to current schema.
- **`.catalog`**: V4 → V5 (encryption) when applicable.
- **`PSetCatalog.storage`**: V1 → V2 (encryption) when opened explicitly (the sample does this after `new Storage`).
- **`PSetProject.storage`**: V1 → V2 when `Storage.PSetStorage` is used.

Use the **same `StorageOptions.EncryptionKey`** when opening migrated DBs (sample default: built-in passphrase; match what you used when creating encrypted DBs).

## Prerequisites

- **.NET 8** + MAUI workload
- **Data / Data.Sync** DLLs in `DataAndSyncDlls\droid\...` (see repo layout)

## UI flow

1. **Sign In** → Home  
2. **Create or open 4 DBs** — creates all four on first run, or opens + migrates existing.  
3. **Insert sample PSet library** — optional test row in `PSetProject.storage`.  
4. **Refresh list** — lists PSet libraries.
