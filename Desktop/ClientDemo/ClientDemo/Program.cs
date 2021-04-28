#define QA
namespace TCConsole
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Trimble.Connect.PSet.Client;
    using Trimble.Identity;
    using Trimble.Identity.OAuth;
    using Trimble.Identity.OAuth.AuthCode;

    /// <summary>
    /// The main class of the program.
    /// </summary>
    public class Program
    {
#if QA
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string ServiceUri = "https://app.qa.connect.trimble.com/tc/api/2.0/";

        /// <summary>
        /// The app URI.
        /// </summary>
        private const string AppUri = "https://app.qa.connect.trimble.com/tc/app";

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = AuthorityUris.StagingUri;

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

        /// <summary>
        /// The ID of a well known existing PSet library.
        /// </summary>
        private static readonly string WellKnonwLibraryID = string.Empty;

        /// <summary>
        /// The ID of a well known existing PSet definition.
        /// </summary>
        private static readonly string WellKnonwDefinitionID = string.Empty;

#elif STAGE
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string ServiceUri = "https://app.stage.connect.trimble.com/tc/api/2.0/";

        /// <summary>
        /// The app URI.
        /// </summary>
        private const string AppUri = "https://app.stage.connect.trimble.com/tc/app";

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = AuthorityUris.StagingUri;

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

        /// <summary>
        /// The ID of a well known existing PSet library.
        /// </summary>
        private static readonly string WellKnonwLibraryID = "";

        /// <summary>
        /// The ID of a well known existing PSet definition.
        /// </summary>
        private static readonly string WellKnonwDefinitionID = "";

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string ServiceUri = "https://app.connect.trimble.com/tc/api/2.0/";

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = AuthorityUris.ProductionUri;

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

        /// <summary>
        /// The ID of a well known existing PSet library.
        /// </summary>
        private static readonly string WellKnonwLibraryID = "3330b592e0d441a598bfe2fe82262aec";

        /// <summary>
        /// The ID of a well known existing PSet definition.
        /// </summary>
        private static readonly string WellKnonwDefinitionID = "QuickAccessItem";
#endif

        readonly static string[] Scopes = new string[] { ClientCredentials.Name };

        /// <summary>
        /// The main method (Entry point).
        /// </summary>
        /// <param name="args">The command line arguments passed to the console app.</param>
        private static void Main(string[] args)
        {
            Run();
            Console.ReadLine();
        }

        /// <summary>
        /// Implements the main demo functionality.
        /// </summary>
        private static async void Run()
        {
            var authCtx = new AuthenticationContext(ClientCredentials, new TokenCache()) { AuthorityUri = new Uri(AuthorityUri) };
            ICredentialsProvider credentialsProvider = new AuthCodeCredentialsProvider(authCtx);

            try
            {
                Console.WriteLine("Acquiring TID token...");
                var token = await authCtx.AcquireTokenAsync(new InteractiveAuthenticationRequest()
                {
                    Scope = $"openid {string.Join(" ", Scopes)}"
                });

                using (var client = new TrimbleConnectClient(new TrimbleConnectClientConfig { ServiceURI = new Uri(ServiceUri) }, credentialsProvider))
                {
                    Console.WriteLine("Logging in to TCPS as {0}...", token.UserInfo.DisplayableId);

                    Console.WriteLine("Projects:");
                    var projects = (await client.GetProjectsAsync()).ToArray();
                    foreach (var p in projects)
                    {
                        Console.WriteLine($"\t{p.Name}");
                    }

                    var project = projects.FirstOrDefault();
                    if (project == null)
                    {
                        Console.WriteLine("You have no projects.");
                        return;
                    }

                    Console.WriteLine("Selected project: " + project.Name);

                    // get project specific wrapper
                    var projectClient = await client.GetProjectClientAsync(project);

                    // ----------
                    // working with todos
                    // ----------
                    Console.WriteLine("Issues:");
                    var todos = await projectClient.Todos.GetAllAsync();
                    foreach (var todo in todos)
                    {
                        Console.WriteLine("\t" + todo.Label + " : " + todo.Description);
                    }

                    // ----------
                    // working with files
                    // ----------
                    Console.WriteLine("Root folder content:");
                    var files = (await projectClient.Files.GetFolderItemsAsync(project.RootFolderIdentifier)).ToArray();
                    foreach (var f in files)
                    {
                        Console.WriteLine("\t" + f.Name + " : " + f.Size + " bytes");
                    }

                    var file = files.FirstOrDefault(f => f.Type == EntityType.File);
                    if (file == null)
                    {
                        Console.WriteLine("No files in the root folder.");
                    }
                    else
                    {
                        // ----------
                        // downloading file content
                        // ----------
                        Console.Write("Downloading '{0}'.", file.Name);
                        using (var stream = await projectClient.Files.DownloadAsync(
                            file.Identifier, 
                            progress: args =>
                            {
                                Console.Write(".");
                            }))
                        {
                            var destPath = Path.GetTempFileName();
                            using (var destination = File.Create(destPath))
                            {
                                await stream.CopyToAsync(destination);
                            }
                        }

                        Console.WriteLine("finished.");
                    }

                    // ----------
                    // working with the PSet service
                    // ----------
                    // we have a well known library in the Europe region, so we work with the European service region specifically.
                    using (var psetClient = new PSetClient(new PSetClientConfig { Region = "europe" }, credentialsProvider))
                    {
                        try
                        {
                            // Get a well known PSet definition
                            var getDefinitionRequest = new GetDefinitionRequest
                            {
                                LibraryId = WellKnonwLibraryID,
                                DefinitionId = WellKnonwDefinitionID,
                            };

                            Definition definition = await psetClient.GetDefinitionAsync(getDefinitionRequest).ConfigureAwait(false);
                            Console.WriteLine($"Got PSet definition: {definition.Id}");

                            // List the PSet instances that exist for the definition and are accessible by the user
                            var listAllPSetsRequest = new ListPSetsRequest
                            {
                                LibraryId = definition.LibraryId,
                                DefinitionId = definition.Id,
                            };

                            var allPSets = new List<PSet>();
                            await psetClient.ListAllPSetsAsync(
                                listAllPSetsRequest,
                                (PSetsPage psetsPage) =>
                                {
                                    if (psetsPage != null && psetsPage.Items != null)
                                    {
                                        allPSets.AddRange(psetsPage.Items);
                                    }
                                }).ConfigureAwait(false);

                            if (allPSets.Count > 0)
                            {
                                Console.WriteLine("Got PSets:");
                                foreach (var pset in allPSets)
                                {
                                    Console.WriteLine($"LibID={pset.LibraryId} DefID={pset.DefinitionId} Link={pset.Link}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No PSets found.");
                            }
                        }
                        catch (InvalidServiceOperationException e)
                        {
                            Console.WriteLine($"PSet service error: {e.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Done.");
        }
    }
}
