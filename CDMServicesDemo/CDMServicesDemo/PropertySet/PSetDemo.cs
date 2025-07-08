//-----------------------------------------------------------------------
// <copyright file="PSetDemo.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Trimble.Connect.Client;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class contains basic functionality.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// The Property Set service client.
        /// </summary>
        private IPSetClient psetClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSetDemo"/> class.
        /// </summary>
        /// <param name="credentialsProvider">The credentials provider.</param>
        public PSetDemo(ICredentialsProvider credentialsProvider)
        {
            Debug.Assert(credentialsProvider != null, "The credentials provider must previously be created and provided.");

            // Create the Property Set service client based on the Property Set service URL specified in the Config
            // and based on the credentials provider that was previously created.
            this.psetClient = new PSetClient(
                new PSetClientConfig { ServiceURI = new Uri(Config.PSetServiceUrl) },
                credentialsProvider);

            // Create the change set client used to handle large amounts of data.
            this.changeSetClient = new System.Net.Http.HttpClient();
        }

        /// <summary>
        /// The main method demonstrating the Property Set service .NET SDK capabilities.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        public async Task Run()
        {
            Console.WriteLine("----- Starting the Property Set service .NET SDK demo... -----");
            Console.WriteLine();

            // Demonstrate library related functionality
            await this.RunLibraryDemo();

            // Demonstrate how to work with additional properties in requests and responses
            await this.RunAdditionalPropertiesDemo();

            // Demonstrate how to work with generic requests
            await this.RunGenericRequestsDemo();

            Console.WriteLine("----- Finished the Property Set service .NET SDK demo. -----");
            Console.WriteLine();
        }
    }
}