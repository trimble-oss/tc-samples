using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Models;
using Trimble.Connect.Data.Sync;
using Trimble.Connect.PSet.Client;
using DataPSet = Trimble.Connect.Data.Models.PSet;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Sample console application demonstrating how to sync PSet libraries using Trimble Connect SDK.
    /// This application shows PSet library, definition, and PSet synchronization for Windows desktop clients.
    /// </summary>
    class Program
    {
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

        static string GetAppSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        /// <summary>
        /// Returns true if the value looks like a placeholder (same as Other Samples: &lt;ClientID&gt;, &lt;ClientKey&gt;, &lt;Name&gt;, etc.).
        /// </summary>
        static bool IsPlaceholder(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var v = value.Trim();
            return v.StartsWith("<") && v.EndsWith(">") ||
                   v.Contains("YOUR_") || v.Contains("ENTER_YOUR") || v.Contains("_HERE") ||
                   v.Equals("ENTER_YOUR_TOKEN_HERE", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("ENTER_YOUR_PROJECT_ID_HERE", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("YOUR_ACCESS_TOKEN_HERE", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("YOUR_PROJECT_ID_HERE", StringComparison.OrdinalIgnoreCase);
        }

        static async Task RunAsync()
        {
            // Step 1: Obtain access token via OAuth browser login (same as Other Samples - uses Config)
            Console.WriteLine("Step 1: Signing in with Trimble Identity...");
            var accessToken = GetAppSetting("AccessToken", null);
            if (string.IsNullOrWhiteSpace(accessToken) || IsPlaceholder(accessToken))
            {
                var missing = new List<string>();
                if (IsPlaceholder(Config.ClientId) || string.IsNullOrWhiteSpace(Config.ClientId)) missing.Add("ClientId");
                if (IsPlaceholder(Config.ClientKey) || string.IsNullOrWhiteSpace(Config.ClientKey)) missing.Add("ClientKey");
                if (string.IsNullOrWhiteSpace(Config.RedirectUrl)) missing.Add("RedirectUrl");
                if (missing.Count > 0)
                {
                    throw new InvalidOperationException(
                        "OAuth not configured. The following are missing or still placeholder in App.config: " + string.Join(", ", missing) + ".\n\n" +
                        "Edit App.config in the project folder (PSetSync.ConsoleApp\\App.config), set ClientId, ClientKey, and RedirectUrl (same values as Other Samples), then rebuild the project so the config is copied to bin\\Debug.");
                }
                Console.WriteLine("Opening browser for Trimble Identity sign-in...");
                var scope = IsPlaceholder(Config.AppName) ? "openid" : "openid " + Config.AppName;
                accessToken = await TrimbleOAuthHelper.GetAccessTokenViaBrowserAsync(
                    Config.ClientId, Config.ClientKey, Config.RedirectUrl,
                    scope, Config.AuthorityUrl).ConfigureAwait(false);
                Console.WriteLine("✓ Sign-in successful.\n");
            }
            else
            {
                Console.WriteLine("✓ Using access token from App.config.\n");
            }

            var projectId = GetAppSetting("ProjectId", null);
            if (string.IsNullOrWhiteSpace(projectId) || IsPlaceholder(projectId))
                projectId = null;

            // Step 2: Create Trimble Connect client with credentials provider (SyncClient requires ICredentialsProvider)
            Console.WriteLine("Step 2: Creating Trimble Connect client...");
            var serviceUri = Config.ConnectServiceUrl.TrimEnd('/') + "/";
            var credentialsProvider = new AccessTokenCredentialsProvider(accessToken);
            var config = new SimpleConnectClientConfig(serviceUri);
            var client = new TrimbleConnectClient(config, credentialsProvider);
            try
            {
                await client.InitializeTrimbleConnectUserAsync();
            }
            catch (Exception ex) when (ex.Message.Contains("text/html") || ex.Message.Contains("MediaTypeFormatter"))
            {
                throw new InvalidOperationException(
                    "The server returned an HTML page instead of JSON. Try:\n" +
                    "  (1) Use a valid Trimble Identity OAuth 2.0 access token (do NOT add 'Bearer '). See: https://developer.trimble.com/docs/authentication/api\n" +
                    "  (2) In App.config set ConnectServiceUrl to try another host (e.g. https://app.connect.trimble.com/tc/api/2.0/).\n" +
                    "  (3) Ensure token and ServiceUri match the same environment (production vs sandbox).\n" +
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

            // Step 3: Create sync client (remote storage)
            Console.WriteLine("Step 3: Creating sync client...");
            var remoteStorage = await SyncClient.CreateAsync(projectClient);
            Console.WriteLine($"✓ Sync client created\n");

            // Step 4: Create local storage
            Console.WriteLine("Step 4: Setting up local storage...");
            var localStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TrimbleConnect",
                "PSetSync",
                projectId);
            
            Directory.CreateDirectory(localStoragePath);
            Console.WriteLine($"✓ Local storage: {localStoragePath}\n");

            // Step 5: Choose Pull or Push (Trimble Connect .NET SDK: Data.Sync PullAsync vs PushAsync)
            Console.WriteLine("Choose operation (Trimble.Connect.Data.Sync):");
            Console.WriteLine("  1. Pull PSets  (Data.Sync PullAsync; optional PSet Client fallback if Sync returns 0)");
            Console.WriteLine("  2. Push PSets  (Data.Sync PushAsync - push modified local PSets to remote)");
            Console.Write("Enter 1 or 2: ");
            var choice = Console.ReadLine()?.Trim();
            if (choice != "1" && choice != "2")
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

            Console.Write("Enter Definition ID (optional - leave blank for all definitions): ");
            var definitionId = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(definitionId)) definitionId = null;

            IStorage localStorage;
            try
            {
                localStorage = await remoteStorage.CreateStorageAsync(localStoragePath);
            }
            catch (InvalidOperationException ex) when (ex.Message?.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("Using existing local storage.");
                localStorage = new Storage(localStoragePath, null);
            }

            using (localStorage)
            {
                var resolvedLibraryId = await EnsureLibraryAndDefinitionsInStorageAsync(remoteStorage, localStorage, libraryId);
                if (resolvedLibraryId == null)
                {
                    Console.WriteLine("Could not resolve or add library. Exiting.");
                    return;
                }

                if (choice == "1")
                {
                    // --- PULL: Data.Sync PullAsync (source logged so you know if PullAsync worked or PSet Client fallback was used)
                    Console.WriteLine($"\nPulling PSets from library {resolvedLibraryId}" + (definitionId != null ? $" definition {definitionId}" : " (all definitions)") + "...");
                    var psets = await PullPSetsAsync(remoteStorage, localStorage, resolvedLibraryId, definitionId);
                    Console.WriteLine($"✓ Pulled {psets.Count} PSets\n");
                    Console.WriteLine("Pulled PSets:");
                    DisplayPSets(localStoragePath, localStorage, resolvedLibraryId, definitionId);
                }
                else
                {
                    // --- PUSH: Data.Sync PushAsync (push modified local PSets to remote)
                    Console.WriteLine($"\nPushing PSets for library {resolvedLibraryId}...");
                    await PushPSetsAsync(remoteStorage, localStorage, resolvedLibraryId);
                    Console.WriteLine("\n✓ Push done.");
                }
            }

            Console.WriteLine("\n✓ Done.");
        }

        /// <summary>
        /// Lists PSet libraries known to local storage (from PSet storage) and their definitions (from PSet Client API).
        /// </summary>
        static async Task ListPSetLibrariesAsync(string localStoragePath, IStorage localStorage, IPSetClient psetClient)
        {
            List<string> libraryIds;
            using (var psetStorage = new PSetProjectStorage(localStoragePath, localStorage as Storage))
            {
                libraryIds = psetStorage.PSetLibraries.Select(l => l.Identifier).ToList();
            }
            if (libraryIds == null || libraryIds.Count == 0)
            {
                Console.WriteLine("  No PSet libraries in local storage yet. Enter a library ID when prompted to pull, or add a library in Trimble Connect first.");
                return;
            }

            Console.WriteLine("Available PSet Libraries (from storage):");
            foreach (var libraryId in libraryIds)
            {
                try
                {
                    var library = await psetClient.GetLibraryAsync(new GetLibraryRequest { LibraryId = libraryId });
                    if (library == null) continue;

                    Console.WriteLine($"  • {library.Name} (ID: {library.Id})");
                    Console.WriteLine($"    Description: {library.Description ?? "N/A"}");

                    // List definitions using PSet Client ListDefinitionsAsync (paged)
                    var allDefs = new System.Collections.Generic.List<Definition>();
                    await psetClient.ListAllDefinitionsAsync(
                        new ListDefinitionsRequest { LibraryId = library.Id },
                        page => { if (page?.Items != null) allDefs.AddRange(page.Items); },
                        default);

                    if (allDefs.Any())
                    {
                        Console.WriteLine($"    Definitions: {allDefs.Count}");
                        foreach (var def in allDefs.Take(5))
                        {
                            Console.WriteLine($"      - {def.Name} (ID: {def.Id})");
                        }
                        if (allDefs.Count > 5)
                        {
                            Console.WriteLine($"      ... and {allDefs.Count - 5} more");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  • Library ID: {libraryId} (could not load: {ex.Message})");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Ensures the library and its definitions exist in local storage (required for SyncClient to pull PSets).
        /// Resolves library by ID (or by name if ID not found) and returns the canonical library Id for use in PullAsync.
        /// </summary>
        static async Task<string> EnsureLibraryAndDefinitionsInStorageAsync(
            SyncClient remoteStorage,
            IStorage localStorage,
            string libraryIdOrName)
        {
            var psetClient = remoteStorage.PSetClient;
            if (psetClient == null)
            {
                Console.WriteLine("  PSet client is not available. Cannot ensure library in storage.");
                return null;
            }

            Library library = null;
            try
            {
                library = await psetClient.GetLibraryAsync(new GetLibraryRequest { LibraryId = libraryIdOrName }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Could not get library '{libraryIdOrName}': {ex.Message}");
            }

            if (library == null)
            {
                Console.WriteLine($"  Library '{libraryIdOrName}' not found in project.");
                return null;
            }

            var storage = localStorage as Storage;
            if (storage?.PSetStorage == null)
            {
                Console.WriteLine("  Local storage does not support PSet. Cannot ensure library.");
                return library.Id;
            }

            var libs = storage.PSetStorage.PSetLibraries;
            var libId = library.Id;
            if (libs.Get(libId) == null)
            {
                var localLib = Translator.ToLocalPSetLibrary(library);
                libs.Insert(localLib, isRemoteState: true);
                Console.WriteLine($"  Added library to local storage: {library.Name} ({libId})");
            }

            var defs = storage.PSetStorage.PSetDefinitions;
            var allDefs = new List<Definition>();
            await psetClient.ListAllDefinitionsAsync(
                new ListDefinitionsRequest { LibraryId = libId },
                page => { if (page?.Items != null) allDefs.AddRange(page.Items); },
                default).ConfigureAwait(false);
            foreach (var def in allDefs)
            {
                if (defs.Get(def.Id) == null)
                {
                    var localDef = Translator.ToLocalPSetDefinition(def);
                    defs.Insert(localDef, isRemoteState: true);
                }
            }
            if (allDefs.Count > 0)
                Console.WriteLine($"  Ensured {allDefs.Count} definition(s) in local storage.");

            return libId;
        }

        /// <summary>
        /// Pulls PSets from a specific library (Trimble.Connect.Data.Sync).
        /// 1) Data.Sync PullAsync is always tried first; log shows SOURCE: PullAsync worked (N &gt; 0) or returned 0.
        /// 2) If PullAsync returns 0, optional FALLBACK uses PSet Client (SyncFullPSetsRequest or ListAllPSetsAsync).
        /// When definitionId is set, API is called with /libs/{libId}/defs/{defId}/psets (definition-scoped).
        /// </summary>
        static async Task<List<PSetEntity>> PullPSetsAsync(
            SyncClient remoteStorage, 
            IStorage localStorage, 
            string libraryId,
            string definitionId = null)
        {
            long progressCount = 0;
            var progress = new Progress<Trimble.Connect.Data.Models.SyncProgressEventArgs<Trimble.Connect.Data.Models.PSet>>(e =>
            {
                if (e != null) progressCount += e.Count;
            });

            // --- SOURCE: Data.Sync PullAsync (Trimble.Connect.Data.Sync.SyncClient.PullAsync)
            // When definitionId is set, sync uses definition-scoped URI: /libs/{libId}/defs/{defId}/psets
            var psets = await remoteStorage.PullAsync(
                (IStorageState)localStorage,
                libraryId,
                definitionId: definitionId,
                pageSize: null,
                progress,
                default);

            var psetsList = psets?.ToList() ?? new List<PSetEntity>();

            // Log SOURCE so you know whether PullAsync worked (N > 0) or returned 0
            if (psetsList.Count > 0)
                Console.WriteLine($"  [SOURCE: Data.Sync PullAsync] returned {psetsList.Count} PSets — PullAsync worked.");
            else
                Console.WriteLine($"  [SOURCE: Data.Sync PullAsync] returned 0 PSets — PullAsync returned nothing.");

            // --- FALLBACK: PSet Client API (only when PullAsync returned 0; controlled by UsePSetClientFallback in App.config)
            // If fallback runs and returns PSets, data came from PSet Client ListAllPSetsAsync, not from PullAsync.
            var useFallback = IsTrue(GetAppSetting("UsePSetClientFallback", "true"));
            if (psetsList.Count == 0 && useFallback)
            {
                var fromClient = await PullPSetsViaClientAsync(remoteStorage, localStorage, libraryId, definitionId).ConfigureAwait(false);
                if (fromClient != null && fromClient.Count > 0)
                {
                    psetsList = fromClient;
                    Console.WriteLine($"  [SOURCE: PSet Client ListAllPSetsAsync] fallback fetched {psetsList.Count} PSets — PSet Client worked, PullAsync did not.");
                }
                else if (fromClient != null)
                    Console.WriteLine($"  [SOURCE: PSet Client] fallback returned 0 PSets.");
            }
            else if (psetsList.Count == 0 && !useFallback)
                Console.WriteLine($"  Fallback disabled (UsePSetClientFallback in App.config). Set to true to use PSet Client when PullAsync returns 0.");

            if (progressCount > 0 && psetsList.Count == 0)
                Console.WriteLine($"  (Sync progress reported {progressCount} items; returned collection was empty.)");

            return psetsList;
        }

        static bool IsTrue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
        }

        /// <summary>
        /// FALLBACK: Fetches PSets via PSet Client SyncFullPSetsRequest + ReceiveAllAsync (SDK-aligned).
        /// When definitionId is set, request uses definition-scoped URI: /libs/{libId}/defs/{defId}/psets.
        /// When definitionId is null, uses library-scoped URI: /libs/{libId}/psets (all definitions).
        /// </summary>
        static async Task<List<PSetEntity>> PullPSetsViaClientAsync(
            SyncClient remoteStorage,
            IStorage localStorage,
            string libraryId,
            string definitionId = null)
        {
            var psetClient = remoteStorage.PSetClient;
            if (psetClient == null) return null;

            var storage = localStorage as Storage;
            if (storage?.PSetStorage?.PSets == null) return null;

            var allPages = new List<SyncablePSetsPage>();
            var request = new SyncFullPSetsRequest
            {
                LibraryId = libraryId,
                DefinitionId = definitionId,
                Top = 500
            };

            await psetClient.ReceiveAllAsync<SyncablePSetsPage>(
                request,
                page =>
                {
                    if (page?.Items != null)
                        allPages.Add(page);
                },
                default).ConfigureAwait(false);

            var result = new List<PSetEntity>();
            foreach (var page in allPages)
            {
                if (page?.Items == null) continue;
                foreach (var remotePset in page.Items)
                {
                    if (remotePset == null) continue;
                    var localPset = Translator.ToLocalPSet(remotePset);
                    if (localPset == null) continue;
                    try
                    {
                        storage.PSetStorage.PSets.Insert(localPset, isRemoteState: true);
                        result.Add(localPset);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: could not insert PSet LinkId={remotePset.Link}, DefId={remotePset.DefinitionId}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Pushes modified local PSets for a library to the remote storage (Trimble.Connect.Data.Sync.SyncClient.PushAsync).
        /// Only PSets that are modified locally are pushed; PSetLibrary and PSetDefinition are read-only.
        /// </summary>
        static async Task PushPSetsAsync(
            SyncClient remoteStorage,
            IStorage localStorage,
            string libraryId)
        {
            var pushCount = 0;
            await remoteStorage.PushAsync(
                (IStorageState)localStorage,
                libraryId,
                callback: entity =>
                {
                    if (entity != null)
                    {
                        pushCount++;
                        var p = entity as DataPSet;
                        Console.WriteLine($"  Pushed: LinkId={p?.LinkId}, DefId={p?.DefId}");
                    }
                },
                error: ex =>
                {
                    if (ex != null)
                        Console.WriteLine($"  Push error: {ex.Message}");
                },
                default).ConfigureAwait(false);
            Console.WriteLine($"  [SOURCE: Data.Sync PushAsync] pushed {pushCount} PSet(s) to remote.");
        }

        /// <summary>
        /// Displays PSets from local storage for a specific library (and optionally definition). Data API: PSet has LinkId, LibId, DefId, PSetProps.
        /// </summary>
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
                    foreach (var pset in defGroup.Take(10))
                    {
                        var modified = pset.Modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                        Console.WriteLine($"    • Link: {pset.LinkId}");
                        Console.WriteLine($"      Modified: {modified}, Version: {pset.Version}");
                    }
                    if (defGroup.Count() > 10)
                        Console.WriteLine($"    ... and {defGroup.Count() - 10} more PSets");
                }
            }
        }

        /// <summary>
        /// Creates a test PSet in local storage (Data API: PSet has LinkId, LibId, DefId, PSetProps).
        /// </summary>
        static async Task CreateTestPSetAsync(
            string localStoragePath,
            IStorage localStorage, 
            IPSetClient psetClient,
            string libraryId)
        {
            // Get definitions for this library using PSet Client ListAllDefinitionsAsync
            var allDefs = new List<Definition>();
            await psetClient.ListAllDefinitionsAsync(
                new ListDefinitionsRequest { LibraryId = libraryId },
                page => { if (page?.Items != null) allDefs.AddRange(page.Items); },
                default);

            if (allDefs.Count == 0)
            {
                Console.WriteLine("No definitions found. Please create a definition first.");
                return;
            }

            var definition = allDefs.First();
            Console.WriteLine($"Using definition: {definition.Name} ({definition.Id})");

            var linkId = $"frn:test:pset:{Guid.NewGuid()}";
            var storage = localStorage as Storage;
            if (storage == null) return;
            using (var psetStorage = new PSetProjectStorage(localStoragePath, storage))
            {
                var newPSet = new DataPSet
                {
                    LinkId = linkId,
                    LibId = libraryId,
                    DefId = definition.Id,
                    Version = 1,
                    PSetProps = "{\"testProperty\": \"testValue\"}",
                    Created = DateTimeOffset.UtcNow,
                    Modified = DateTimeOffset.UtcNow
                };

                psetStorage.PSets.Insert(newPSet, isRemoteState: false);
                Console.WriteLine($"✓ Created test PSet with link: {linkId}");
                Console.WriteLine("  (Will be pushed on next sync)");
            }
        }
    }
}
