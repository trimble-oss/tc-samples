//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Trimble.Connect.Client;
    using Trimble.Identity.OAuth;
    using Trimble.Identity.OAuth.Password;

    /// <summary>
    /// The main program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The main method (application entry point).
        /// </summary>
        /// <param name="args">The arguments passed to the console application.</param>
        private static void Main(string[] args)
        {
            Task.Run(() => Run());

            Console.ReadLine();
        }

        /// <summary>
        /// The main demo method.
        /// </summary>
        private static async void Run()
        {
            Console.WriteLine("Starting CDM services demo...");
            Console.WriteLine();

            Console.WriteLine("Initializing CDM services clients...");
            Console.WriteLine();

            // First create a credentials provider using the values specified in the Config.
            // A single credentials provider can be used to create multiple service clients.           
            ICredentialsProvider credentialsProvider = new PasswordCredentialsProvider(
                new Uri(Config.AuthorityUrl),
                new ClientCredentials(Config.ClientId, Config.ClientKey),
                new NetworkCredential(Config.UserName, Config.UserPassword));

            // Create the Organizer demo instance, which internally initializes the Organizer client.
            OrgDemo orgDemo = new OrgDemo(credentialsProvider);

            // Create the Property Set demo instance, which internally initializes the Property Set client.
            PSetDemo psetDemo = new PSetDemo(credentialsProvider);

            // Run the Organizer service .NET SDK demo...
            await orgDemo.Run().ConfigureAwait(false);

            // Run the Property Set service .NET SDK demo...
            await psetDemo.Run().ConfigureAwait(false);

            Console.WriteLine("Finished CDM services demo.");
        }
    }
}
