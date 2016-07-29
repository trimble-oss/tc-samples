namespace TCConsole
{
    using System;
    using System.IO;
    using System.Linq;

    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Trimble.Identity;

    class Program
    {
#if true
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string ServiceUri = "https://app.stage.connect.trimble.com/tc/api/2.0/";

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = "https://identity-stg.trimble.com/i/oauth2/";

        /// <summary>
        /// The client creadentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string ServiceUri = "https://app.prod.gteam.com/tc/api/2.0/";

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = "https://identity.trimble.com/i/oauth2/";

        /// <summary>
        /// The client creadentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };
#endif

        static void Main(string[] args)
        {
            Run();
            Console.ReadLine();
        }

        static async void Run()
        {
            var authCtx = new AuthenticationContext(ClientCredentials, new TokenCache()) { AuthorityUri =new Uri(AuthorityUri) };
            try
            {
                Console.WriteLine("Acquiring TID token...");
                var token = await authCtx.AcquireTokenAsync();
                using (var client = new TrimbleConnectClient(ServiceUri))
                {
                    Console.WriteLine("Logging in to TCPS as {0}...", token.UserInfo.DisplayableId);
                    await client.LoginAsync(token.IdToken);

                    Console.WriteLine("Projects:");
                    var projects = (await client.GetProjectsAsync()).ToArray();
                    foreach (var p in projects)
                    {
                        Console.WriteLine("\t" + p.Name);
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

                    //
                    // working with todos
                    //
                    Console.WriteLine("Issues:");
                    var todos = await projectClient.Todos.GetAllAsync();
                    foreach (var todo in todos)
                    {
                        Console.WriteLine("\t" + todo.Label + " : " + todo.Description);
                    }

                    //
                    // working with files
                    //
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
                        //
                        // downloading file content
                        //
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
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
