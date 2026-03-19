using System;
using System.Collections.Generic;
using System.IO;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Models;

namespace DataSyncSampleApp.Services;

/// <summary>
/// Aligns with PSetSync.ConsoleApp: four DBs in one project folder — .storage, .catalog, PSetCatalog.storage, PSetProject.storage.
/// Opening with <see cref="StorageOptions.EncryptionKey"/> runs SDK migrations (unencrypted → encrypted: main V100→V101, catalog V4→V5, PSet V1→V2).
/// </summary>
public static class LocalStorageService
{
    private const string StorageFolderName = "TCDataSyncSample";

    /// <summary>Same filenames as Trimble.Connect.Data (project folder).</summary>
    public static class DbFiles
    {
        public const string MainStorage = ".storage";
        public const string Catalog = ".catalog";
        public const string PSetCatalog = "PSetCatalog.storage";
        public const string PSetProject = "PSetProject.storage";
    }

    public static string GetStorageRootDirectory()
    {
        return Path.Combine(FileSystem.AppDataDirectory, StorageFolderName);
    }

    /// <summary>
    /// Builds StorageOptions. If encryptionKey is null or empty, SDK uses its certificate-based default key.
    /// </summary>
    public static StorageOptions BuildOptions(string? encryptionKey = null)
    {
        // If no key provided, return empty StorageOptions - SDK will use certificate-based default
        if (string.IsNullOrEmpty(encryptionKey))
        {
            return new StorageOptions();
        }
        
        // If key provided, use it
        return new StorageOptions
        {
            EncryptionKey = encryptionKey
        };
    }

    /// <summary>
    /// Creates all four databases (new install) or opens existing and runs encryption/schema migrations as needed.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>new Storage(path, options)</c> migrates .storage (e.g. V100→V101 SQLCipher) and opens .catalog (V4→V5).</description></item>
    /// <item><description>Existing <c>PSetCatalog.storage</c> is opened explicitly — Storage does not migrate a pre-existing v1 file otherwise.</description></item>
    /// <item><description><c>storage.PSetStorage</c> opens <c>PSetProject.storage</c> and migrates v1→v2 if needed.</description></item>
    /// </list>
    /// </remarks>
    public static FourDatabaseInitResult InitializeFourDatabases(string projectDir, string? encryptionKey = null)
    {
        var options = BuildOptions(encryptionKey);
        Directory.CreateDirectory(projectDir);
        var log = new List<string>();
        var person = new Person
        {
            Identifier = "sample-user",
            Email = "sample@trimble.com",
            FirstName = "Sample",
            LastName = "User"
        };
        var project = new Project
        {
            Identifier = "sample-project",
            Name = "Sample Project"
        };

        Storage storage;
        bool createdNew;

        if (!Storage.Exists(projectDir))
        {
            storage = Storage.Create(projectDir, person, project, options);
            createdNew = true;
            log.Add("New install: created .storage, .catalog, PSetCatalog.storage, PSetProject.storage (encrypted, current schema).");
        }
        else
        {
            createdNew = false;
            storage = new Storage(projectDir, options);
            log.Add("Opened .storage — SDK migrates unencrypted/older schema to encrypted current version when applicable (e.g. V100→V101).");
            log.Add("Catalog opened with Storage — .catalog migrates V4→V5 (encryption) when applicable.");

            var psetCatalogPath = Path.Combine(projectDir, DbFiles.PSetCatalog);
            if (File.Exists(psetCatalogPath))
            {
                using (var psetCat = new PSetCatalog(projectDir, options))
                {
                    log.Add($"PSetCatalog.storage opened — schema v{psetCat.SchemaVersion} (V1→V2 encryption migration if was unencrypted v1).");
                }
            }
            else
            {
                log.Add("PSetCatalog.storage was absent; Storage ctor should have created it if needed.");
            }

            var psetProjectPath = Path.Combine(projectDir, DbFiles.PSetProject);
            if (File.Exists(psetProjectPath))
            {
                var pset = storage.PSetStorage;
                log.Add($"PSetProject.storage opened — schema v{pset.SchemaVersion} (V1→V2 encryption migration if was unencrypted v1).");
            }
            else
            {
                _ = storage.PSetStorage;
                log.Add("PSetProject.storage ensured via Storage.PSetStorage.");
            }
        }

        var versions = ReadFourDatabaseVersions(projectDir, storage, options);
        return new FourDatabaseInitResult(storage, createdNew, log, versions);
    }

    private static FourDatabaseVersions ReadFourDatabaseVersions(string projectDir, Storage storage, StorageOptions options)
    {
        int main = storage.SchemaVersion;
        int catalog = storage.Catalog?.SchemaVersion ?? -1;
        int psetCat = -1;
        if (File.Exists(Path.Combine(projectDir, DbFiles.PSetCatalog)))
        {
            using (var pc = new PSetCatalog(projectDir, options))
                psetCat = pc.SchemaVersion;
        }

        int psetProj = storage.PSetStorage?.SchemaVersion ?? -1;
        return new FourDatabaseVersions(main, catalog, psetCat, psetProj);
    }

    public static IReadOnlyList<PSetLibraryInfo> GetPSetLibraries(Storage storage)
    {
        var list = new List<PSetLibraryInfo>();
        if (storage == null) return list;

        try
        {
            var psetStorage = storage.PSetStorage;
            if (psetStorage?.PSetLibraries == null) return list;

            foreach (PSetLibrary lib in psetStorage.PSetLibraries)
                list.Add(new PSetLibraryInfo(lib.Identifier, lib.LibName));
        }
        catch (Exception)
        {
            // ignore
        }

        return list;
    }
}

public sealed class FourDatabaseVersions
{
    public FourDatabaseVersions(int mainStorage, int catalog, int psetCatalog, int psetProject)
    {
        MainStorage = mainStorage;
        Catalog = catalog;
        PSetCatalog = psetCatalog;
        PSetProject = psetProject;
    }

    public int MainStorage { get; }
    public int Catalog { get; }
    public int PSetCatalog { get; }
    public int PSetProject { get; }

    public override string ToString()
    {
        return $".storage PRAGMA user_version={MainStorage}, .catalog={Catalog}, PSetCatalog={PSetCatalog}, PSetProject={PSetProject}";
    }
}

public sealed class FourDatabaseInitResult : IDisposable
{
    public FourDatabaseInitResult(Storage storage, bool createdNew, IReadOnlyList<string> log, FourDatabaseVersions versions)
    {
        Storage = storage;
        CreatedNewInstallation = createdNew;
        Log = log;
        Versions = versions;
    }

    public Storage Storage { get; }
    public bool CreatedNewInstallation { get; }
    public IReadOnlyList<string> Log { get; }
    public FourDatabaseVersions Versions { get; }

    public void Dispose() => Storage?.Dispose();
}

public record PSetLibraryInfo(string Identifier, string LibName);
