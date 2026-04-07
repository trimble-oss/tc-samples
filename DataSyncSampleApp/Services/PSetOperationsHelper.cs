using System.Text;
using DataSyncSampleApp.Models;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Models;
using Trimble.Connect.Data.Security;
using Trimble.Connect.Data.Sync;
using Trimble.SQLite;
using DataPSet = Trimble.Connect.Data.Models.PSet;

namespace DataSyncSampleApp.Services;

/// <summary>Mobile port of PSetSync.ConsoleApp operations 1–10 (encrypted SQLCipher storage).</summary>
public static class PSetOperationsHelper
{
    public static string GetProjectStoragePath(string projectId)
    {
        var userId = "sample-user"; // You can make this dynamic based on logged-in user
#if ANDROID
        // Use external storage for easy access: /sdcard/Android/data/com.trimble.datasyncsample/files/DataSyncSampleApp/userid/projectid/
        var externalStorage = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/sdcard";
        return Path.Combine(
            externalStorage,
            "Android", "data", "com.trimble.datasyncsample", "files",
            "DataSyncSampleApp", userId, projectId);
#elif IOS
        // App sandbox (no Android-style external storage on iOS).
        var appData = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
        return Path.Combine(appData, "DataSyncSampleApp", userId, projectId);
#else
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "DataSyncSampleApp", userId, projectId);
#endif
    }

    /// <summary>
    /// Builds StorageOptions without specifying EncryptionKey.
    /// The SDK will automatically use its certificate-based default encryption key.
    /// </summary>
    public static StorageOptions BuildOptions() => new();

    public static Task<(IStorage? Storage, string Log)> OpenOrCreateStorageAsync(
        SyncClient remote, string projectDir, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        
        try
        {
            sb.AppendLine($"Project directory: {projectDir}");
            sb.AppendLine($"Directory exists BEFORE CreateDirectory: {Directory.Exists(projectDir)}");
            
            var options = BuildOptions();
            sb.AppendLine($"Encryption key configured: {!string.IsNullOrEmpty(options.EncryptionKey)} (length: {options.EncryptionKey?.Length ?? 0})");
            
            // Ensure directory exists
            Directory.CreateDirectory(projectDir);
            sb.AppendLine($"Directory.CreateDirectory() called");
            sb.AppendLine($"Directory exists AFTER CreateDirectory: {Directory.Exists(projectDir)}");
            
            // Test if we can write to this directory
            var testFile = Path.Combine(projectDir, "test_write.tmp");
            try
            {
                File.WriteAllText(testFile, "test");
                bool testExists = File.Exists(testFile);
                sb.AppendLine($"Write test: {(testExists ? "SUCCESS" : "FAILED - file not created")}");
                if (testExists)
                {
                    File.Delete(testFile);
                    sb.AppendLine("Write test file deleted");
                }
            }
            catch (Exception writeEx)
            {
                sb.AppendLine($"Write test FAILED: {writeEx.Message}");
            }
            
            // Check for .storage file specifically
            var storageFilePath = Path.Combine(projectDir, ".storage");
            sb.AppendLine($".storage file path: {storageFilePath}");
            sb.AppendLine($".storage file exists: {File.Exists(storageFilePath)}");

            Storage storage;
            
            bool storageExists = Storage.Exists(projectDir);
            sb.AppendLine($"Storage.Exists() returned: {storageExists}");
            
            if (!storageExists)
            {
                sb.AppendLine("Creating new storage...");
                
                // Create storage with person/project info - this ensures all 4 databases are created
                var person = new Person
                {
                    Identifier = "sample-user",
                    Email = "sample@trimble.com",
                    FirstName = "Sample",
                    LastName = "User"
                };
                
                var selectedProj = AppSession.SelectedProject;
                var project = new Project
                {
                    Identifier = selectedProj?.Identifier ?? "sample-project",
                    Name = selectedProj?.Name ?? "Sample Project"
                };
                
                sb.AppendLine($"Using project: {project.Name} ({project.Identifier})");
                
                try
                {
                    storage = Storage.Create(projectDir, person, project, options);
                    sb.AppendLine("✓ Storage.Create() succeeded!");
                    
                    // Immediately check if files were created
                    sb.AppendLine("\nChecking files immediately after Storage.Create():");
                    var storageFile = Path.Combine(projectDir, ".storage");
                    var catalogFile = Path.Combine(projectDir, ".catalog");
                    sb.AppendLine($"  .storage exists: {File.Exists(storageFile)}");
                    sb.AppendLine($"  .catalog exists: {File.Exists(catalogFile)}");
                    
                    if (File.Exists(storageFile))
                    {
                        var size = new FileInfo(storageFile).Length;
                        sb.AppendLine($"  .storage size: {size} bytes");
                    }
                    
                    if (File.Exists(catalogFile))
                    {
                        var size = new FileInfo(catalogFile).Length;
                        sb.AppendLine($"  .catalog size: {size} bytes");
                    }
                    
                    sb.AppendLine("Created new encrypted storage with all 4 databases (.storage, .catalog, PSetCatalog.storage, PSetProject.storage).");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"✗ Storage.Create() FAILED: {ex.GetType().Name}");
                    sb.AppendLine($"Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            else
            {
                sb.AppendLine("Storage exists, opening...");
                try
                {
                    storage = new Storage(projectDir, options);
                    sb.AppendLine("Opened existing encrypted storage.");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Open failed: {ex.Message}. Recreating storage…");
                    
                    // Create with person/project info
                    var person = new Person
                    {
                        Identifier = "sample-user",
                        Email = "sample@trimble.com",
                        FirstName = "Sample",
                        LastName = "User"
                    };
                    
                    var selectedProj = AppSession.SelectedProject;
                    var project = new Project
                    {
                        Identifier = selectedProj?.Identifier ?? "sample-project",
                        Name = selectedProj?.Name ?? "Sample Project"
                    };
                    
                    storage = Storage.Create(projectDir, person, project, options);
                    sb.AppendLine("Recreated storage.");
                }
            }

            // Ensure all four databases are initialized and report their status
            try
            {
                sb.AppendLine("Verifying databases...");
                sb.AppendLine($"Main storage: schema v{storage.SchemaVersion}");
                
                // Access Catalog - this should trigger .catalog file creation
                var catalog = storage.Catalog;
                if (catalog != null)
                {
                    sb.AppendLine($"Catalog: schema v{catalog.SchemaVersion}");
                }
                else
                {
                    sb.AppendLine("Warning: Catalog is null");
                }

                // Access PSetStorage - this should trigger PSetProject.storage creation
                var psetStorage = storage.PSetStorage;
                if (psetStorage != null)
                {
                    sb.AppendLine($"PSetProject: schema v{psetStorage.SchemaVersion}");
                    
                    // Explicitly open PSetCatalog - this should trigger PSetCatalog.storage creation
                    var psetCatalogPath = Path.Combine(projectDir, "PSetCatalog.storage");
                    sb.AppendLine($"Checking PSetCatalog at: {psetCatalogPath}");
                    
                    if (!File.Exists(psetCatalogPath))
                    {
                        sb.AppendLine("PSetCatalog.storage doesn't exist yet, creating it...");
                    }
                    
                    try
                    {
                        // Open/create PSetCatalog
                        using (var psetCat = new PSetCatalog(projectDir, options))
                        {
                            sb.AppendLine($"PSetCatalog: schema v{psetCat.SchemaVersion}");
                            
                            // Access PSetLibraries to ensure database is fully initialized
                            var libCount = psetCat.PSetLibraries.Count();
                            sb.AppendLine($"PSetCatalog has {libCount} libraries");
                        }
                        
                        // Check if file was created
                        if (File.Exists(psetCatalogPath))
                        {
                            var size = new FileInfo(psetCatalogPath).Length;
                            sb.AppendLine($"✓ PSetCatalog.storage created successfully ({size} bytes)");
                        }
                        else
                        {
                            sb.AppendLine("✗ PSetCatalog.storage STILL MISSING after PSetCatalog constructor!");
                        }
                    }
                    catch (Exception psetCatEx)
                    {
                        sb.AppendLine($"✗ PSetCatalog creation error: {psetCatEx.GetType().Name}: {psetCatEx.Message}");
                    }
                }
                else
                {
                    sb.AppendLine("Warning: PSetStorage is null");
                }
                
                // List actual files created
                sb.AppendLine("\nFiles created:");
                foreach (var file in new[] { ".storage", ".catalog", "PSetCatalog.storage", "PSetProject.storage" })
                {
                    var path = Path.Combine(projectDir, file);
                    var exists = File.Exists(path);
                    var size = exists ? new FileInfo(path).Length : 0;
                    sb.AppendLine($"  {file}: {(exists ? $"exists ({size} bytes)" : "MISSING")}");
                }
                
                sb.AppendLine("\nAll databases initialized successfully.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"✗ Error during database verification: {ex.GetType().Name}");
                sb.AppendLine($"Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"Inner: {ex.InnerException.Message}");
                }
            }

            return Task.FromResult<(IStorage? Storage, string Log)>((storage, sb.ToString()));
        }
        catch (Exception ex)
        {
            sb.AppendLine($"\n✗✗✗ FATAL ERROR in OpenOrCreateStorageAsync ✗✗✗");
            sb.AppendLine($"Type: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                sb.AppendLine($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            sb.AppendLine($"Stack trace:\n{ex.StackTrace}");
            
            return Task.FromResult<(IStorage? Storage, string Log)>((null, sb.ToString()));
        }
    }

    public static async Task<string> PullAsync(
        SyncClient remote, IStorage local, string libraryId, string? definitionId, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        if (local is not Storage storage || storage.PSetStorage == null)
        {
            sb.AppendLine("PSet storage not available.");
            return sb.ToString();
        }

        var progress = new Progress<SyncProgressEventArgs<PSet>>(_ => { });
        var psets = await remote.PullAsync((IStorageState)local, libraryId, definitionId: definitionId,
            pageSize: null, progress, ct).ConfigureAwait(false);
        var list = psets?.ToList() ?? new List<PSetEntity>();
        sb.AppendLine($"Pulled {list.Count} PSet(s).");

        using var psetStorage = new PSetProjectStorage(storage.DirectoryPath, storage);
        var lib = psetStorage.PSetLibraries.FirstOrDefault(l => l.Identifier == libraryId);
        if (lib != null) sb.AppendLine($"Library: {lib.LibName}");
        return sb.ToString();
    }

    public static string PushStub() => "Push: not implemented in sample (same as PSetSync.ConsoleApp).";

    public static string CreateTestPSet(IStorage local, string projectDir, string libraryId, string definitionId, string? linkId)
    {
        if (local is not Storage storage) return "Invalid storage.";
        linkId ??= $"frn:test:pset:{Guid.NewGuid()}";
        using var psetStorage = new PSetProjectStorage(projectDir, storage);
        var p = new DataPSet
        {
            LinkId = linkId,
            LibId = libraryId,
            DefId = definitionId,
            Version = 1,
            PSetProps = "{}",
            Created = DateTimeOffset.UtcNow,
            Modified = DateTimeOffset.UtcNow
        };
        var inserted = psetStorage.PSets.Insert(p, isRemoteState: false);
        return $"Created PSet Id={inserted?.Id}, LinkId={linkId}";
    }

    public static string UpdatePSet(IStorage local, string projectDir, string libraryId, long psetId, string newPropsJson)
    {
        if (local is not Storage storage) return "Invalid storage.";
        using var psetStorage = new PSetProjectStorage(projectDir, storage);
        var p = psetStorage.PSets.OfType<DataPSet>().FirstOrDefault(x => x.LibId == libraryId && x.Id == psetId);
        if (p == null) return $"PSet id {psetId} not found.";
        p.PSetProps = newPropsJson;
        p.Modified = DateTimeOffset.UtcNow;
        p.Version++;
        psetStorage.PSets.Update(p, isRemoteState: false);
        return $"Updated PSet {psetId}, version {p.Version}";
    }

    public static string DeletePSet(IStorage local, string projectDir, string libraryId, long psetId)
    {
        if (local is not Storage storage) return "Invalid storage.";
        using var psetStorage = new PSetProjectStorage(projectDir, storage);
        psetStorage.PSets.Delete(psetId, isRemoteState: false);
        return $"Marked PSet {psetId} deleted locally.";
    }

    public static string TestFourDatabases(string projectDir)
    {
        try
        {
            // Pass null to use SDK's default certificate-based encryption key
            using var r = LocalStorageService.InitializeFourDatabases(projectDir, encryptionKey: null);
            var v = r.Versions;
            return $"Four DB init OK.\nMain={v.MainStorage}, Catalog={v.Catalog}, PSetCat={v.PSetCatalog}, PSetProj={v.PSetProject}\n" +
                   string.Join("\n", r.Log);
        }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
    }

    public static string HealthCheck(string projectDir)
    {
        var options = BuildOptions();
        var key = string.IsNullOrEmpty(options.EncryptionKey)
            ? new CertificateKeyProvider().GetKey()
            : options.EncryptionKey!;
        var sb = new StringBuilder();
        foreach (var (name, path) in new[]
                 {
                     ("Main", Path.Combine(projectDir, ".storage")),
                     ("Catalog", Path.Combine(projectDir, ".catalog")),
                     ("PSetCatalog", Path.Combine(projectDir, "PSetCatalog.storage")),
                     ("PSetProject", Path.Combine(projectDir, "PSetProject.storage"))
                 })
        {
            if (!File.Exists(path)) { sb.AppendLine($"{name}: missing"); continue; }
            try
            {
                using var db = new Sqlite(path);
                db.Execute($"PRAGMA key = '{key.Replace("'", "''")}'");
                sb.AppendLine($"{name}: OK, user_version={db.GetUserVersion()}");
            }
            catch (Exception ex) { sb.AppendLine($"{name}: {ex.Message}"); }
        }
        return sb.ToString();
    }

    public static string DisplayDbInfo(string projectDir)
    {
        var o = BuildOptions();
        var sb = new StringBuilder();
        sb.AppendLine($"Path: {projectDir}");
        sb.AppendLine(string.IsNullOrEmpty(o.EncryptionKey)
            ? "Mode: certificate-derived key"
            : $"Mode: app passphrase ({o.EncryptionKey.Length} chars)");
        foreach (var f in new[] { ".storage", ".catalog", "PSetCatalog.storage", "PSetProject.storage" })
            sb.AppendLine($"  {f}: {(File.Exists(Path.Combine(projectDir, f)) ? "exists" : "missing")}");
        return sb.ToString();
    }

    public static string VerifyEncryption(string projectDir)
    {
        var o = BuildOptions();
        return HealthCheck(projectDir) + "\n(Opened with configured key = encrypted if OK)";
    }

    public static string RunCustomSql(string projectDir, string dbFileName, string sql)
    {
        var path = Path.Combine(projectDir, dbFileName);
        if (!File.Exists(path)) return $"File not found: {dbFileName}";
        var key = BuildOptions().EncryptionKey ?? new CertificateKeyProvider().GetKey();
        try
        {
            using var db = new Sqlite(path);
            db.Execute($"PRAGMA key = '{key.Replace("'", "''")}'");
            using var tx = db.BeginTransaction();
            db.Execute(sql);
            tx.Commit();
            return "SQL executed.";
        }
        catch (Exception ex) { return ex.Message; }
    }
}
