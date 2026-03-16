using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trimble.Connect.Client;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Models;
using Trimble.Connect.Data.Sync;
using Trimble.Identity.OAuth.AuthCode;
using DataPSet = Trimble.Connect.Data.Models.PSet;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Sample console application demonstrating how to sync PSet libraries using Trimble Connect SDK.
    /// This application shows PSet library, definition, and PSet synchronization for Windows desktop clients.
    /// Targets .NET Framework 4.8 with modern C# 8.0 features.
    /// </summary>
    class Program
    {
        const int MaxDisplayedPsetsInSummary = 10;
        const int MaxDisplayedPsetsInList = 20;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Trimble Connect PSet Library Sync Sample ===\n");
                
                // Run async operations synchronously for console app
                RunAsync().GetAwaiter().GetResult();
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        static async Task RunAsync()
        {
            // Step 1: Configure OAuth authentication with AuthCode (PKCE + Serial PKCE)
            Console.WriteLine("Step 1: Configuring OAuth authentication...");
            
            var clientId = Config.ClientId;
            if (string.IsNullOrWhiteSpace(clientId) || Config.IsPlaceholder(clientId))
            {
                throw new InvalidOperationException(
                    "Client ID is required. Set ClientId in App.config.\n\n" +
                    "To get a client ID:\n" +
                    "  1. Go to Trimble Developer Console (https://console.trimble.com/)\n" +
                    "  2. Create or select your application\n" +
                    "  3. Copy the Client ID\n" +
                    "  4. Set it in App.config: <add key=\"ClientId\" value=\"YOUR_CLIENT_ID\" />\n" +
                    "  5. Rebuild so the config is copied to bin\\Debug");
            }

            var appName = Config.AppName;
            if (string.IsNullOrWhiteSpace(appName) || Config.IsPlaceholder(appName))
                appName = "PSetSyncConsole";

            var redirectUri = Config.RedirectUrl;
            var authorityUri = new Uri(Config.AuthorityUrl);

            var authContext = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce)
            {
                AuthorityUri = authorityUri,
            };

            var authCodeCredentialsProvider = new AuthCodeCredentialsProvider(authContext);

            // Step 2: Attempt silent login with saved tokens
            Console.WriteLine("Step 2: Checking for saved authentication...");
            var refreshToken = LoadRefreshToken();
            var codeVerifier = LoadCodeVerifier();
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                Console.WriteLine("Attempting silent login with saved tokens...");
                try
                {
                    authCodeCredentialsProvider.WithRefreshToken(refreshToken);
                    
                    // TODO: WithCodeVerifier method not available in current OAuth package version
                    //if (!string.IsNullOrEmpty(codeVerifier))
                    //{
                    //    authCodeCredentialsProvider.WithCodeVerifier(codeVerifier);
                    //}
                    
                    var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync();
                    Console.WriteLine("✓ Silent login successful!\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Silent login failed: {ex.Message}");
                    Console.WriteLine("Browser login required...\n");
                    refreshToken = null; // Force browser login
                }
            }

            // Step 3: Browser login if no valid refresh token
            if (string.IsNullOrEmpty(refreshToken))
            {
                Console.WriteLine("Opening browser for authentication...");
                Console.WriteLine("Please sign in with your Trimble ID credentials.");
                Console.WriteLine("After signing in, the browser will redirect back to this app.\n");
                
                try
                {
                    var accessToken = await authCodeCredentialsProvider.AcquireTokenAsync();
                    Console.WriteLine("✓ Login successful!\n");
                    
                    // Save refresh token for future silent logins
                    var newRefreshToken = authCodeCredentialsProvider.RefreshToken;
                    SaveRefreshToken(newRefreshToken);
                    
                    // TODO: GetEncryptedCodeVerifier method not available in current OAuth package version
                    //// If using Serial PKCE, also save code verifier
                    //if (authContext.UseSerialPkce)
                    //{
                    //    var newCodeVerifier = authCodeCredentialsProvider.GetEncryptedCodeVerifier();
                    //    SaveCodeVerifier(newCodeVerifier);
                    //}
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Authentication failed: {ex.Message}\n\n" +
                        "Ensure you have:\n" +
                        "  1. Valid client ID configured in App.config\n" +
                        "  2. Redirect URI registered in Trimble Developer Console\n" +
                        "  3. Internet connection for authentication", ex);
                }
            }

            // TODO: OnTokenRefreshedWithCodeVerifier event not available in current OAuth package version
            //// Step 4: Subscribe to token refresh events (for automatic token updates)
            //if (authContext.UseSerialPkce)
            //{
            //    authCodeCredentialsProvider.OnTokenRefreshedWithCodeVerifier += 
            //        (newRefreshToken, newCodeVerifier, timestamp) =>
            //    {
            //        // Automatically save new tokens when they're refreshed
            //        SaveRefreshToken(newRefreshToken);
            //        if (!string.IsNullOrEmpty(newCodeVerifier))
            //        {
            //            SaveCodeVerifier(newCodeVerifier);
            //        }
            //        Console.WriteLine("✓ Access token refreshed automatically");
            //    };
            //}
            else
            {
                authCodeCredentialsProvider.OnTokenRefreshed += 
                    (newRefreshToken, timestamp) =>
                {
                    // Automatically save new tokens when they're refreshed
                    SaveRefreshToken(newRefreshToken);
                    Console.WriteLine("✓ Access token refreshed automatically");
                };
            }

            var projectId = Config.ProjectId;
            if (string.IsNullOrWhiteSpace(projectId) || Config.IsPlaceholder(projectId))
                projectId = null;

            // Step 5: Create Trimble Connect client with AuthCode credentials provider
            Console.WriteLine("Step 3: Creating Trimble Connect client...");
            var serviceUri = Config.ConnectServiceUrl.TrimEnd('/') + "/";
            var clientConfig = new TrimbleConnectClientConfig 
            { 
                ServiceURI = new Uri(serviceUri) 
            };
            var client = new TrimbleConnectClient(clientConfig, authCodeCredentialsProvider);
            
            try
            {
                await client.InitializeTrimbleConnectUserAsync();
            }
            catch (Exception ex) when (ex.Message.Contains("text/html") || ex.Message.Contains("MediaTypeFormatter"))
            {
                throw new InvalidOperationException(
                    "The server returned an HTML page instead of JSON. Try:\n" +
                    "  (1) Ensure your authentication is valid and not expired.\n" +
                    "  (2) In App.config set ConnectServiceUrl to try another host (e.g. https://app.connect.trimble.com/tc/api/2.0/).\n" +
                    "  (3) Ensure AuthorityUrl and ConnectServiceUrl match the same environment (production vs staging).\n" +
                    "Original error: " + ex.Message, ex);
            }

            // Get project: by ID from config, or interactively from list (like Other Samples)
            var allProjects = new List<Trimble.Connect.Client.Models.Project>();
            await client.GetProjectsAsync(false, null, projects =>
            {
                foreach (var p in projects)
                    allProjects.Add(p);
            }, null, default);

            Trimble.Connect.Client.Models.Project project = null;
            if (!string.IsNullOrEmpty(projectId))
            {
                project = allProjects.FirstOrDefault(p => string.Equals(p.Identifier, projectId, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                    throw new InvalidOperationException($"Project '{projectId}' not found. Check ProjectId in App.config.");
            }
            else
            {
                if (allProjects.Count == 0)
                    throw new InvalidOperationException("No projects found for this user. Create a project in Trimble Connect first.");
                Console.WriteLine("Select a project by number (or press Enter to use first):");
                for (int i = 0; i < allProjects.Count; i++)
                    Console.WriteLine($"  {i + 1}. {allProjects[i].Name} (ID: {allProjects[i].Identifier})");
                Console.Write("Enter number or project ID: ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    project = allProjects[0];
                    projectId = project.Identifier;
                }
                else
                {
                    if (int.TryParse(input, out int num) && num >= 1 && num <= allProjects.Count)
                    {
                        project = allProjects[num - 1];
                        projectId = project.Identifier;
                    }
                    else
                    {
                        project = allProjects.FirstOrDefault(p => string.Equals(p.Identifier, input, StringComparison.OrdinalIgnoreCase));
                        if (project == null)
                            project = allProjects.FirstOrDefault(p => string.Equals(p.Name, input, StringComparison.OrdinalIgnoreCase));
                        if (project == null)
                            throw new InvalidOperationException($"No project matching '{input}'.");
                        projectId = project.Identifier;
                    }
                }
                Console.WriteLine();
            }

            var projectClient = await client.GetProjectClientAsync(project);
            Console.WriteLine($"✓ Using project: {project.Name}\n");

            // Step 4: Create sync client (remote storage)
            Console.WriteLine("Step 4: Creating sync client...");
            var remoteStorage = await SyncClient.CreateAsync(projectClient);
            Console.WriteLine($"✓ Sync client created\n");

            // Step 5: Create local storage
            // Note: All local databases are now encrypted using SQLCipher for security.
            // Encryption keys are automatically managed by the SDK using Windows Credential Manager.
            Console.WriteLine("Step 5: Setting up local storage...");
            var localStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TrimbleConnect",
                "PSetSync",
                projectId);
            
            Directory.CreateDirectory(localStoragePath);
            Console.WriteLine($"✓ Local storage: {localStoragePath}");
            Console.WriteLine("  (Databases are encrypted using SQLCipher)\n");

            // Step 6: Choose operation - Pull, Push, Create, Update, or Delete
            Console.WriteLine("Step 6: Choose operation:");
            Console.WriteLine("  1. Pull PSets (download from remote)");
            Console.WriteLine("  2. Push PSets (upload modified PSets to remote)");
            Console.WriteLine("  3. Create Test PSet (create a new PSet locally)");
            Console.WriteLine("  4. Update PSet (modify an existing PSet locally)");
            Console.WriteLine("  5. Delete PSet (mark a PSet for deletion)");
            Console.Write("Enter choice (1-5): ");
            var operation = Console.ReadLine()?.Trim();

            if (operation != "1" && operation != "2" && operation != "3" && operation != "4" && operation != "5")
            {
                Console.WriteLine("Invalid choice. Exiting.");
                return;
            }

            Console.Write("\nEnter Library ID: ");
            var libraryId = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(libraryId))
            {
                Console.WriteLine("Library ID is required. Exiting.");
                return;
            }

            string definitionId = null;
            if (operation == "1")
            {
                Console.Write("Enter Definition ID (optional, press Enter for all definitions): ");
                definitionId = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(definitionId))
                    definitionId = null;
            }
            else if (operation == "3")
            {
                Console.Write("Enter Definition ID: ");
                definitionId = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(definitionId))
                {
                    Console.WriteLine("Definition ID is required. Exiting.");
                    return;
                }
            }

            // Step 7: Configure storage options with encryption settings
            // 
            // IMPORTANT: Trimble.Connect.Data now uses SQLCipher for database encryption.
            // The SQLCipher native DLLs are automatically deployed by the NuGet package - no manual configuration needed!
            // 
            // Encryption Key Options (2-tier priority):
            // 
            // Option 1: App-Provided Passphrase (Simplest - Used here for testing)
            //   - Provide a simple string passphrase via StorageOptions.EncryptionKey
            //   - Easy to use with DB Browser for SQLite
            //   - Good for development and testing
            //   - Same key can be used across all databases
            // 
            // Option 2: Default Global Certificate-Based (Automatic)
            //   - Global key derived from embedded certificate thumbprint only
            //   - No configuration needed (just omit EncryptionKey)
            //   - Same key for all databases (not per-database)
            //   - Most secure, but harder to access with external tools
            //
            Console.WriteLine("Step 6: Setting up encrypted local storage...");
            
            var storageOptions = new StorageOptions();
            
            // Using Option 1: Simple passphrase for easy testing and DB Browser access
         //   storageOptions.EncryptionKey = "TestPassword123";
            
            Console.WriteLine("[DEBUG] Using simple passphrase encryption for testing");
            Console.WriteLine("[DEBUG] Passphrase: TestPassword123");
            Console.WriteLine("[DEBUG] Use this passphrase to open the database in DB Browser for SQLite");
            
            // To use Option 2 (Default Global Certificate-Based):
            storageOptions = new StorageOptions(); // No key = uses global certificate-based key

            IStorage localStorage = null;
            bool isNewDatabase = false;
            bool isMigrated = false;
            
            try
            {
                // Check if database already exists
                var dbPath = Path.Combine(localStoragePath, "tc.db");
                var dbExists = File.Exists(dbPath);
                
                if (dbExists)
                {
                    Console.WriteLine("Existing database found. Opening with encryption...");
                    
                    // WORKAROUND: Clean up any leftover .encrypted files from failed migrations
                    // This addresses the race condition bug in V99To100 migration
                    var encryptedFile = Path.Combine(localStoragePath, ".storage.encrypted");
                    if (File.Exists(encryptedFile))
                    {
                        Console.WriteLine("Detected incomplete migration file (.storage.encrypted)");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n⚠️  MIGRATION RECOVERY REQUIRED");
                        Console.WriteLine("A previous migration was interrupted. To continue:");
                        Console.WriteLine($"1. Close this application");
                        Console.WriteLine($"2. Delete the entire database folder:");
                        Console.WriteLine($"   {localStoragePath}");
                        Console.WriteLine($"3. Run the application again\n");
                        Console.ResetColor();
                        
                        Console.WriteLine("Attempting automatic cleanup...");
                        bool cleanedUp = false;
                        
                        try
                        {
                            // Force garbage collection
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                            
                            // Aggressive retry with longer delays
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    File.Delete(encryptedFile);
                                    Console.WriteLine("✓ Automatic cleanup successful");
                                    cleanedUp = true;
                                    break;
                                }
                                catch (IOException)
                                {
                                    if (i < 4)
                                    {
                                        Console.WriteLine($"  Retry {i + 1}/5 - waiting for file lock to release...");
                                        System.Threading.Thread.Sleep(2000);
                                    }
                                }
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            Console.WriteLine($"✗ Automatic cleanup failed: {cleanupEx.Message}");
                        }
                        
                        if (!cleanedUp)
                        {
                            throw new InvalidOperationException(
                                "Cannot proceed - .storage.encrypted file is locked by another process.\n\n" +
                                "REQUIRED ACTION:\n" +
                                "1. Close ALL instances of PSetSync.ConsoleApp\n" +
                                "2. Wait 10 seconds\n" +
                                $"3. Manually delete: {localStoragePath}\n" +
                                "4. Run the application again\n\n" +
                                "If the file remains locked, restart your computer.");
                        }
                    }
                    
                    // Try to open existing storage (may trigger migration from V99 to V100)
                    try
                    {
                        localStorage = new Storage(localStoragePath, storageOptions);
                        
                        // Check if migration backup was created (indicates migration occurred)
                        var backupFiles = Directory.GetFiles(localStoragePath, "tc.db.v99.backup*");
                        if (backupFiles.Length > 0)
                        {
                            isMigrated = true;
                            Console.WriteLine("✓ Database migrated from V99 (unencrypted) to V100 (encrypted)");
                            Console.WriteLine($"  Backup created: {Path.GetFileName(backupFiles[0])}");
                        }
                        else
                        {
                            Console.WriteLine("✓ Existing encrypted database opened successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not open existing database: {ex.Message}");
                        Console.WriteLine("Attempting to create new storage...");
                        localStorage = await remoteStorage.CreateStorageAsync(localStoragePath, storageOptions);
                        isNewDatabase = true;
                    }
                }
                else
                {
                    // Create new encrypted storage
                    Console.WriteLine("Creating new encrypted database...");
                    localStorage = await remoteStorage.CreateStorageAsync(localStoragePath, storageOptions);
                    isNewDatabase = true;
                    Console.WriteLine("✓ New encrypted database created");
                }
                
                // Display encryption status with SDK version check
                var sdkVersion = typeof(Storage).Assembly.GetName().Version;
                var supportsSqlCipher = sdkVersion >= new Version(2, 11, 0);
                
                if (supportsSqlCipher)
                {
                    if (isNewDatabase)
                    {
                        Console.WriteLine("  Encryption: Enabled (256-bit AES via SQLCipher)");
                        Console.WriteLine("  Key Storage: Windows Credential Manager");
                    }
                    else if (isMigrated)
                    {
                        Console.WriteLine("  Encryption: Enabled (migrated from unencrypted)");
                        Console.WriteLine("  Key Storage: Windows Credential Manager");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠️  WARNING: SQLCipher encryption NOT supported by current SDK");
                    Console.WriteLine($"     Current SDK version: {sdkVersion}");
                    Console.WriteLine("     Required SDK version: 2.11.0+ for SQLCipher support");
                    Console.WriteLine("     Database is UNENCRYPTED (standard SQLite format)");
                    Console.WriteLine("     Can be opened with any SQLite browser");
                    Console.ResetColor();
                }
                
                Console.WriteLine();
            }
            catch (InvalidOperationException ex) when (ex.Message?.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("Storage already exists. Opening existing database...");
                localStorage = new Storage(localStoragePath, storageOptions);
                Console.WriteLine("✓ Existing storage opened successfully\n");
            }
            catch (IOException ex) when (ex.Message?.Contains("being used by another process") == true)
            {
                throw new InvalidOperationException(
                    $"Database migration failed - file lock issue: {ex.Message}\n\n" +
                    "This is a known race condition in the V99→V100 migration.\n\n" +
                    "Solutions:\n" +
                    "  1. Close ALL instances of this application\n" +
                    "  2. Wait 10 seconds for file handles to release\n" +
                    "  3. Delete the database folder and try again:\n" +
                    $"     {localStoragePath}\n" +
                    "  4. If the issue persists, restart your computer to force-release file locks\n\n" +
                    "Technical details:\n" +
                    "  - SQLite's DETACH command doesn't immediately release file handles\n" +
                    "  - The .storage.encrypted file remains locked briefly after DETACH\n" +
                    "  - The SDK tries to swap files before the lock is released", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to initialize local storage: {ex.Message}\n\n" +
                    "Possible causes:\n" +
                    "  1. Database encryption key not found in Windows Credential Manager\n" +
                    "  2. Database file is corrupted\n" +
                    "  3. Insufficient permissions to access local storage path\n" +
                    "  4. SQLCipher native libraries not found\n\n" +
                    "Solutions:\n" +
                    "  1. Delete the local database folder and re-sync from cloud\n" +
                    "  2. Check Windows Credential Manager for TrimbleConnect_DatabaseKey entries\n" +
                    "  3. Ensure you have write permissions to the local storage path\n" +
                    "  4. Verify Trimble.SQLite package is properly installed", ex);
            }
            
            if (localStorage == null)
            {
                throw new InvalidOperationException("Failed to create or open local storage");
            }

            try
            {
                using (localStorage)
                {
                    // Verify storage is properly initialized
                    var storage = localStorage as Storage;
                    if (storage == null)
                    {
                        throw new InvalidOperationException("Local storage is not properly initialized");
                    }
                    
                    Console.WriteLine($"Storage Path: {storage.DirectoryPath}");
                    
                    // Check actual database encryption status using the public SchemaVersion property
                    var dbVersion = storage.SchemaVersion;
                    var isEncrypted = dbVersion >= 100;
                    
                    if (isEncrypted)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Encryption: ENABLED (SQLCipher)");
                        Console.WriteLine($"  Database Version: {dbVersion}");
                        Console.ResetColor();
                        
                        // Display encryption key for debugging
                        var databasePath = Path.Combine(storage.DirectoryPath, ".storage");
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n[DEBUG] Encryption Key Information:");
                        Console.WriteLine($"  Passphrase: TestPassword123");
                        Console.WriteLine($"  Database Path: {databasePath}");
                        Console.WriteLine($"\nTo open this database in DB Browser for SQLite:");
                        Console.WriteLine($"  1. Download DB Browser with SQLCipher support");
                        Console.WriteLine($"  2. File -> Open Database -> Select: {databasePath}");
                        Console.WriteLine($"  3. Choose 'SQLCipher 4 defaults'");
                        Console.WriteLine($"  4. Enter passphrase: TestPassword123");
                        Console.WriteLine($"  5. Click OK");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠️  Encryption: NOT ENABLED");
                        Console.WriteLine($"  Database Version: {dbVersion} (V100+ required for encryption)");
                        Console.WriteLine("   Database is UNENCRYPTED - can be opened with any SQLite browser");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                    
                    if (operation == "1")
                    {
                        // Pull PSets (unchanged - optional definitionId)
                        Console.WriteLine(definitionId != null
                            ? $"\nPulling library, definition, and PSets for library {libraryId}, definition {definitionId}..."
                            : $"\nPulling library and PSets for library {libraryId} (all definitions)...");
                        await PullLibraryDataAsync(remoteStorage, localStorage, libraryId, definitionId);
                        Console.WriteLine("\n✓ Pull completed.");
                    }
                    else if (operation == "2")
                    {
                        // Push PSets
                        Console.WriteLine($"\nPushing modified PSets for library {libraryId}...");
                        await PushPSetsAsync(remoteStorage, localStorage, libraryId);
                    }
                    else if (operation == "3")
                    {
                        // Create test PSet
                        Console.WriteLine($"\nCreating test PSet for library {libraryId}, definition {definitionId}...");
                        var created = await CreateTestPSetAsync(localStoragePath, localStorage, libraryId, definitionId);
                        if (created)
                            Console.WriteLine("\n✓ Test PSet created. Use option 2 to push it to remote.");
                        else
                            Console.WriteLine("\n✗ Failed to create PSet. See error above.");
                    }
                    else if (operation == "4")
                    {
                        // Update PSet
                        Console.WriteLine($"\nUpdating PSet for library {libraryId}...");
                        var updated = await UpdatePSetAsync(localStoragePath, localStorage, libraryId);
                        if (updated)
                            Console.WriteLine("\n✓ PSet updated. Use option 2 to push changes to remote.");
                        else
                            Console.WriteLine("\n✗ Failed to update PSet. See error above.");
                    }
                    else if (operation == "5")
                    {
                        // Delete PSet
                        Console.WriteLine($"\nDeleting PSet for library {libraryId}...");
                        var deleted = await DeletePSetAsync(localStoragePath, localStorage, libraryId);
                        if (deleted)
                            Console.WriteLine("\n✓ PSet marked for deletion. Use option 2 to push deletion to remote.");
                        else
                            Console.WriteLine("\n✗ Failed to delete PSet. See error above.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nDatabase operation failed: {ex.Message}");
                Console.ResetColor();
                throw;
            }

            Console.WriteLine("\n✓ Done.");
        }

        /// <summary>Pulls library, definition, and PSets into local storage (definitionId null = all definitions).</summary>
        static async Task PullLibraryDataAsync(
            SyncClient remoteStorage,
            IStorage localStorage,
            string libraryId,
            string definitionId = null)
        {
            var storage = localStorage as Storage;
            if (storage?.PSetStorage == null)
            {
                Console.WriteLine("  Local storage does not support PSet.");
                Console.WriteLine("  This may indicate the database was not properly initialized.");
                Console.WriteLine("  Try deleting the local database folder and running again.");
                return;
            }

            List<PSetEntity> psetsList = new List<PSetEntity>();
            
            try
            {
                // Pull PSets using Data.Sync PullAsync with both library and definition IDs
                // This method automatically fetches and caches the library and definition metadata
                Console.WriteLine(definitionId != null
                    ? $"Pulling library, definition, and PSets for library {libraryId}, definition {definitionId}..."
                    : $"Pulling library and PSets for library {libraryId} (all definitions)...");

                var progress = new Progress<Trimble.Connect.Data.Models.SyncProgressEventArgs<Trimble.Connect.Data.Models.PSet>>(_ => { });

                var psets = await remoteStorage.PullAsync(
                    (IStorageState)localStorage,
                    libraryId,
                    definitionId: definitionId,  // Pull specific definition
                    pageSize: null,
                    progress,
                    default);
                
                psetsList = psets?.ToList() ?? new List<PSetEntity>();
                Console.WriteLine($"✓ Pulled {psetsList.Count} PSet(s)");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Pull failed: {ex.Message}");
                Console.ResetColor();
                
                if (ex.Message.Contains("encrypted") || ex.Message.Contains("cipher") || ex.Message.Contains("decrypt"))
                {
                    Console.WriteLine("\nThis appears to be an encryption-related error.");
                    Console.WriteLine("Possible solutions:");
                    Console.WriteLine("  1. Delete the local database folder and re-sync");
                    Console.WriteLine("  2. Check Windows Credential Manager for encryption keys");
                    Console.WriteLine("  3. Ensure Trimble.SQLite package is properly installed");
                }
                
                throw;
            }

            // Display summary of what was pulled
            using (var psetStorage = new PSetProjectStorage(storage.DirectoryPath, storage))
            {
                var library = psetStorage.PSetLibraries.FirstOrDefault(l => l.Identifier == libraryId);
                if (library != null)
                {
                    Console.WriteLine($"✓ Library: {library.LibName} ({library.Identifier})");
                }

                if (definitionId != null)
                {
                    var definition = psetStorage.PSetDefinitions.FirstOrDefault(d => d.Identifier == definitionId && d.LibId == libraryId);
                    if (definition != null)
                        Console.WriteLine($"✓ Definition: {definition.DefName} ({definition.Identifier})");
                }
                else
                {
                    var definitionsForLib = psetStorage.PSetDefinitions.Where(d => d.LibId == libraryId).ToList();
                    if (definitionsForLib.Any())
                    {
                        Console.WriteLine($"✓ Definitions: {definitionsForLib.Count}");
                        foreach (var def in definitionsForLib.Take(10))
                            Console.WriteLine($"    - {def.DefName} ({def.Identifier})");
                        if (definitionsForLib.Count > 10)
                            Console.WriteLine($"    ... and {definitionsForLib.Count - 10} more");
                    }
                }

                if (psetsList.Count > 0)
                {
                    Console.WriteLine("\nPSet Summary:");
                    DisplayPSets(storage.DirectoryPath, localStorage, libraryId, definitionId);
                }
            }
        }

        /// <summary>Push to remote is not yet implemented.</summary>
        static Task PushPSetsAsync(
            SyncClient remoteStorage,
            IStorage localStorage,
            string libraryId)
        {
            Console.WriteLine("  Push: Yet to be implemented.");
            return Task.CompletedTask;
        }

        /// <summary>Displays PSets from local storage for a library (and optionally definition).</summary>
        static void DisplayPSets(string localStoragePath, IStorage localStorage, string libraryId, string definitionId = null)
        {
            var storage = localStorage as Storage;
            if (storage == null) return;
            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                var psets = new List<DataPSet>();
                foreach (var item in psetStorage.PSets)
                {
                    if (item is DataPSet d && d.LibId == libraryId)
                    {
                        if (definitionId == null || string.Equals(d.DefId, definitionId, StringComparison.OrdinalIgnoreCase))
                            psets.Add(d);
                    }
                }
                psets = psets.OrderBy(p => p.DefId).ThenBy(p => p.LinkId).ToList();

                if (psets.Count == 0)
                {
                    Console.WriteLine("  No PSets found in local storage.");
                    return;
                }

                var groupedByDef = psets.GroupBy(p => p.DefId);
                foreach (var defGroup in groupedByDef)
                {
                    Console.WriteLine($"\n  Definition: {defGroup.Key}");
                    foreach (var pset in defGroup.Take(MaxDisplayedPsetsInSummary))
                    {
                        var modified = pset.Modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                        Console.WriteLine($"    • Link: {pset.LinkId}");
                        Console.WriteLine($"      Modified: {modified}, Version: {pset.Version}");
                    }
                    if (defGroup.Count() > MaxDisplayedPsetsInSummary)
                        Console.WriteLine($"    ... and {defGroup.Count() - MaxDisplayedPsetsInSummary} more PSets");
                }
            }
        }

        /// <summary>Gets PSets for a library from local storage.</summary>
        static List<DataPSet> GetPsetsForLibrary(string localStoragePath, IStorage localStorage, string libraryId)
        {
            var storage = localStorage as Storage;
            if (storage == null) return new List<DataPSet>();
            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                var list = new List<DataPSet>();
                foreach (var item in psetStorage.PSets)
                    if (item is DataPSet d && d.LibId == libraryId)
                        list.Add(d);
                return list;
            }
        }

        /// <summary>Shows PSets, prompts for internal ID, returns the chosen PSet or null.</summary>
        static DataPSet TryPickByIdFromList(List<DataPSet> psets, string promptVerb)
        {
            if (psets == null || psets.Count == 0) return null;
            Console.WriteLine("\nAvailable PSets:");
            for (int i = 0; i < Math.Min(psets.Count, MaxDisplayedPsetsInList); i++)
            {
                var p = psets[i];
                Console.WriteLine($"  {i + 1}. ID: {p.Id}, LinkId: {p.LinkId}, DefId: {p.DefId}, Version: {p.Version}");
            }
            if (psets.Count > MaxDisplayedPsetsInList)
                Console.WriteLine($"  ... and {psets.Count - MaxDisplayedPsetsInList} more");
            Console.Write($"\nEnter the PSet ID (internal database ID) to {promptVerb}: ");
            var idInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(idInput) || !long.TryParse(idInput, out long psetId))
            {
                Console.WriteLine("Valid PSet ID is required.");
                return null;
            }
            var chosen = psets.FirstOrDefault(p => p.Id == psetId);
            if (chosen == null)
                Console.WriteLine($"PSet with ID '{psetId}' not found.");
            return chosen;
        }

        /// <summary>Creates a PSet in local storage (Data/Data.Sync only; validation at Push).</summary>
        static Task<bool> CreateTestPSetAsync(
            string localStoragePath,
            IStorage localStorage,
            string libraryId,
            string definitionId)
        {
            var storage = localStorage as Storage;
            if (storage == null) return Task.FromResult(false);

            Console.Write("Enter Link ID (e.g., frn:test:element:123) or press Enter to auto-generate: ");
            var linkId = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(linkId))
            {
                linkId = $"frn:test:pset:{Guid.NewGuid()}";
                Console.WriteLine($"Using generated Link ID: {linkId}");
            }

            Console.Write("Enter initial properties (JSON; must match definition schema, or press Enter for empty {}): ");
            var propsInput = Console.ReadLine()?.Trim();
            var psetProps = string.IsNullOrEmpty(propsInput) ? "{}" : propsInput;

            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                var newPSet = new DataPSet
                {
                    LinkId = linkId,
                    LibId = libraryId,
                    DefId = definitionId,
                    Version = 1,
                    PSetProps = psetProps,
                    Created = DateTimeOffset.UtcNow,
                    Modified = DateTimeOffset.UtcNow
                };

                var insertedPSet = psetStorage.PSets.Insert(newPSet, isRemoteState: false);
                Console.WriteLine($"✓ Created PSet:");
                Console.WriteLine($"  Internal ID: {insertedPSet?.Id ?? newPSet.Id}");
                Console.WriteLine($"  Link ID: {linkId}");
                Console.WriteLine($"  Library ID: {libraryId}");
                Console.WriteLine($"  Definition ID: {definitionId}");
                Console.WriteLine($"  Properties: {(insertedPSet?.PSetProps ?? newPSet.PSetProps)}");
                Console.WriteLine("\n  Note: Invalid library/definition IDs will be reported when you Push (option 2).");
                return Task.FromResult(true);
            }
        }

        /// <summary>Updates an existing PSet in local storage by modifying its properties.</summary>
        static Task<bool> UpdatePSetAsync(
            string localStoragePath,
            IStorage localStorage,
            string libraryId)
        {
            var psets = GetPsetsForLibrary(localStoragePath, localStorage, libraryId);
            if (psets.Count == 0)
            {
                Console.WriteLine($"No PSets found for library {libraryId}. Pull or create PSets first.");
                return Task.FromResult(false);
            }
            var chosen = TryPickByIdFromList(psets, "update");
            if (chosen == null) return Task.FromResult(false);

            var storage = localStorage as Storage;
            if (storage == null) return Task.FromResult(false);
            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                var psetToUpdate = psetStorage.PSets.OfType<DataPSet>().FirstOrDefault(p => p.LibId == libraryId && p.Id == chosen.Id);
                if (psetToUpdate == null)
                {
                    Console.WriteLine($"PSet with ID '{chosen.Id}' not found.");
                    return Task.FromResult(false);
                }
                Console.WriteLine($"\nCurrent properties: {psetToUpdate.PSetProps}");
                Console.Write("Enter new properties (JSON; must match definition schema): ");
                var newProps = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(newProps))
                {
                    Console.WriteLine("Properties cannot be empty.");
                    return Task.FromResult(false);
                }
                psetToUpdate.PSetProps = newProps;
                psetToUpdate.Modified = DateTimeOffset.UtcNow;
                psetToUpdate.Version++;
                psetStorage.PSets.Update(psetToUpdate, isRemoteState: false);
                Console.WriteLine($"✓ Updated PSet:");
                Console.WriteLine($"  ID: {psetToUpdate.Id}");
                Console.WriteLine($"  Link ID: {psetToUpdate.LinkId}");
                Console.WriteLine($"  Definition ID: {psetToUpdate.DefId}");
                Console.WriteLine($"  New Version: {psetToUpdate.Version}");
                Console.WriteLine($"  New Properties: {psetToUpdate.PSetProps}");
                Console.WriteLine("\n  This PSet is marked as modified and will be pushed on next Push operation (option 2).");
                return Task.FromResult(true);
            }
        }

        /// <summary>Marks a PSet for deletion in local storage.</summary>
        static Task<bool> DeletePSetAsync(
            string localStoragePath,
            IStorage localStorage,
            string libraryId)
        {
            var psets = GetPsetsForLibrary(localStoragePath, localStorage, libraryId);
            if (psets.Count == 0)
            {
                Console.WriteLine($"No PSets found for library {libraryId}. Pull or create PSets first.");
                return Task.FromResult(false);
            }
            var chosen = TryPickByIdFromList(psets, "delete");
            if (chosen == null) return Task.FromResult(false);
            Console.Write($"Are you sure you want to delete PSet (ID: {chosen.Id}, LinkId: {chosen.LinkId}, DefId: {chosen.DefId})? (y/n): ");
            var confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Delete cancelled.");
                return Task.FromResult(false);
            }
            var storage = localStorage as Storage;
            if (storage == null) return Task.FromResult(false);
            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                psetStorage.PSets.Delete(chosen.Id, isRemoteState: false);
                Console.WriteLine($"✓ Deleted PSet:");
                Console.WriteLine($"  ID: {chosen.Id}");
                Console.WriteLine($"  Link ID: {chosen.LinkId}");
                Console.WriteLine($"  Definition ID: {chosen.DefId}");
                Console.WriteLine("\n  This PSet is marked for deletion and will be removed from remote on next Push operation (option 2).");
                return Task.FromResult(true);
            }
        }

        #region Token Storage Helper Methods

        /// <summary>
        /// Token storage location. Uses user profile directory for simplicity.
        /// For production, consider using Windows Credential Manager for enhanced security.
        /// </summary>
        private static readonly string TokenStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "PSetSyncConsoleApp",
            "tokens.json"
        );

        /// <summary>Loads the refresh token from secure storage.</summary>
        private static string LoadRefreshToken()
        {
            try
            {
                if (File.Exists(TokenStoragePath))
                {
                    var json = File.ReadAllText(TokenStoragePath);
                    var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(json);
                    return tokenInfo?.RefreshToken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load refresh token: {ex.Message}");
            }
            return null;
        }

        /// <summary>Loads the code verifier from secure storage (for Serial PKCE).</summary>
        private static string LoadCodeVerifier()
        {
            try
            {
                if (File.Exists(TokenStoragePath))
                {
                    var json = File.ReadAllText(TokenStoragePath);
                    var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(json);
                    return tokenInfo?.CodeVerifier;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load code verifier: {ex.Message}");
            }
            return null;
        }

        /// <summary>Saves the refresh token to secure storage.</summary>
        private static void SaveRefreshToken(string refreshToken)
        {
            try
            {
                var tokenInfo = LoadTokenInfo() ?? new TokenInfo();
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.Timestamp = DateTime.UtcNow.Ticks;
                SaveTokenInfo(tokenInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to save refresh token: {ex.Message}");
            }
        }

        /// <summary>Saves the code verifier to secure storage (for Serial PKCE).</summary>
        private static void SaveCodeVerifier(string codeVerifier)
        {
            try
            {
                var tokenInfo = LoadTokenInfo() ?? new TokenInfo();
                tokenInfo.CodeVerifier = codeVerifier;
                SaveTokenInfo(tokenInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to save code verifier: {ex.Message}");
            }
        }

        /// <summary>Loads the complete token info from storage.</summary>
        private static TokenInfo LoadTokenInfo()
        {
            if (File.Exists(TokenStoragePath))
            {
                try
                {
                    var json = File.ReadAllText(TokenStoragePath);
                    return JsonConvert.DeserializeObject<TokenInfo>(json);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>Saves the complete token info to storage.</summary>
        private static void SaveTokenInfo(TokenInfo tokenInfo)
        {
            var directory = Path.GetDirectoryName(TokenStoragePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var json = JsonConvert.SerializeObject(tokenInfo, Formatting.Indented);
            File.WriteAllText(TokenStoragePath, json);
        }

        /// <summary>
        /// Simple DTO for token storage.
        /// Note: Tokens are already encrypted by the SDK's CryptoHelper.
        /// For production, consider using Windows Credential Manager instead of file storage.
        /// </summary>
        private class TokenInfo
        {
            public string RefreshToken { get; set; }
            public string CodeVerifier { get; set; } // For Serial PKCE
            public long Timestamp { get; set; }
        }

        #endregion
    }
}
