﻿//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using System;
    using System.Threading.Tasks;
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
                // Create an authentication context based on the values in the config.
                var authCtx = new AuthContext(Config.ClientId, Config.ClientKey, Config.AppName, Config.RedirectUrl)
                {
                    AuthorityUri = new Uri(Config.AuthorityUrl)
                };

                // First create a credentials based on the previously created authentication context.
                // A single credentials provider can be used to create multiple service clients.           
                AuthCodeCredentialsProvider credentialsProvider = new AuthCodeCredentialsProvider(authCtx);
                //credentialsProvider.AuthenticationRequest = new InteractiveAuthenticationRequest()
                //{
                //    Scope = $"openid {string.Join(" ", Config.ClientCredentials.Name)}"
                //};

                Console.WriteLine("Acquiring TID token...");
                var token = await credentialsProvider.AcquireTokenAsync();
                Console.WriteLine();

               // var logout = await credentialsProvider.Logout();

                // Create the usage examples demo instance, which internally initializes the Organizer and Property Set clients.
                UsageExamplesDemo examplesDemo = new UsageExamplesDemo(credentialsProvider);

                // Run the usage examples demo...
                await examplesDemo.Run().ConfigureAwait(false);
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
