//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Threading.Tasks;
    using Trimble.Identity;
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
        /// <returns>Does not return anything.</returns>
        private static async Task Main(string[] args)
        {
            await Run();

            Console.ReadLine();
        }

        /// <summary>
        /// The main demo method.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        private static async Task Run()
        {
            try
            {
                Console.WriteLine("Starting CDM services demo...");
                Console.WriteLine();

                // Create an authentication context based on the values in the config.
                var authCtx = new AuthenticationContext(Config.ClientCredentials, new TokenCache())
                {
                    AuthorityUri = new Uri(Config.AuthorityUrl)
                };

                // First create a credentials based on the previously created authentication context.
                // A single credentials provider can be used to create multiple service clients.           
                AuthCodeCredentialsProvider credentialsProvider = new AuthCodeCredentialsProvider(authCtx);
                credentialsProvider.AuthenticationRequest = new InteractiveAuthenticationRequest()
                {
                    Scope = $"openid {string.Join(" ", Config.ClientCredentials.Name)}"
                };

                Console.WriteLine("Acquiring TID token...");
                var token = await authCtx.AcquireTokenAsync(credentialsProvider.AuthenticationRequest);

                Console.WriteLine("Initializing CDM services clients...");
                Console.WriteLine();

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
