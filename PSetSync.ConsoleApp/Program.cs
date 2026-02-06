using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Trimble.Connect.Client;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Models;
using Trimble.Connect.Data.Sync;
using DataPSet = Trimble.Connect.Data.Models.PSet;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Sample console application demonstrating how to sync PSet libraries using Trimble Connect SDK.
    /// This application shows PSet library, definition, and PSet synchronization for Windows desktop clients.
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
            // Step 1: Obtain access token via OAuth browser login (uses Config)
            Console.WriteLine("Step 1: Signing in with Trimble Identity...");
            var accessToken = Config.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken) || Config.IsPlaceholder(accessToken))
            {
                // Use effective values (Config applies defaults when App.config has placeholders)
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(Config.ClientId)) missing.Add("ClientId");
                if (string.IsNullOrWhiteSpace(Config.ClientKey)) missing.Add("ClientKey");
                if (string.IsNullOrWhiteSpace(Config.RedirectUrl)) missing.Add("RedirectUrl");
                if (missing.Count > 0)
                {
                    throw new InvalidOperationException(
                        "OAuth not configured. The following are missing in App.config: " + string.Join(", ", missing) + ".\n\n" +
                        "Edit App.config, set ClientId, ClientKey, and RedirectUrl (or leave placeholders to use built-in defaults), then rebuild so the config is copied to bin\\Debug.");
                }
                Console.WriteLine("Opening browser for Trimble Identity sign-in...");
                var scope = string.IsNullOrWhiteSpace(Config.AppName) ? "openid" : "openid " + Config.AppName;
                accessToken = await TrimbleOAuthHelper.GetAccessTokenViaBrowserAsync(
                    Config.ClientId, Config.ClientKey, Config.RedirectUrl,
                    scope, Config.AuthorityUrl).ConfigureAwait(false);
                Console.WriteLine("✓ Sign-in successful.\n");
            }
            else
            {
                Console.WriteLine("✓ Using access token from App.config.\n");
            }

            var projectId = Config.ProjectId;
            if (string.IsNullOrWhiteSpace(projectId) || Config.IsPlaceholder(projectId))
                projectId = null;

            // Step 2: Create Trimble Connect client with credentials provider (SyncClient requires ICredentialsProvider)
            Console.WriteLine("Step 2: Creating Trimble Connect client...");
            var serviceUri = Config.ConnectServiceUrl.TrimEnd('/') + "/";
            var credentialsProvider = new AccessTokenCredentialsProvider(accessToken);
            var clientConfig = new SimpleConnectClientConfig(serviceUri);
            var client = new TrimbleConnectClient(clientConfig, credentialsProvider);
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

            // Step 5: Choose operation - Pull, Push, Create, Update, or Delete
            Console.WriteLine("Step 5: Choose operation:");
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
                return;
            }

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

            var psetsList = psets?.ToList() ?? new List<PSetEntity>();
            Console.WriteLine($"✓ Pulled {psetsList.Count} PSet(s)");

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
    }
}
