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
    using Trimble.Identity;
    using Trimble.Identity.OAuth;
    using Trimble.Identity.OAuth.AuthCode;

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
            try
            {
                Console.WriteLine("Starting CDM services demo...");
                Console.WriteLine();

                // Create an authentication context based on the values in the config.
                var authCtx = new AuthenticationContext(new ClientCredential(Config.ClientId, Config.ClientKey, "TC.SDK.Example"), new TokenCache())
                {
                    AuthorityUri = new Uri(Config.AuthorityUrl)
                };

                Console.WriteLine("Acquiring TID token...");
                var token = await authCtx.AcquireTokenAsync();

                Console.WriteLine("Initializing CDM services clients...");
                Console.WriteLine();

                // First create a credentials based on the previously created authentication context.
                // A single credentials provider can be used to create multiple service clients.           
                ICredentialsProvider credentialsProvider = new AuthCodeCredentialsProvider(authCtx);

                // Create the Organizer demo instance, which internally initializes the Organizer client.
                OrgDemo orgDemo = new OrgDemo(credentialsProvider);

                // Create the Property Set demo instance, which internally initializes the Property Set client.
                PSetDemo psetDemo = new PSetDemo(credentialsProvider);

                // Run the Organizer service .NET SDK demo...
                await orgDemo.Run().ConfigureAwait(false);

                // Run the Property Set service .NET SDK demo...
                await psetDemo.Run().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Finished CDM services demo.");
        }
    }
}
