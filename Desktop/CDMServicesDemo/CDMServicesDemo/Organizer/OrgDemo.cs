//-----------------------------------------------------------------------
// <copyright file="OrgDemo.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Trimble.Connect.Client;
    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class contains basic functionality.
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// The Organizer service client.
        /// </summary>
        private IOrgClient orgClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrgDemo"/> class.
        /// </summary>
        /// <param name="credentialsProvider">The credentials provider.</param>
        public OrgDemo(ICredentialsProvider credentialsProvider)
        {
            Debug.Assert(credentialsProvider != null, "The credentials provider must previously be created and provided.");

            // Create the Organizer service client based on the Organizer service URL specified in the Config
            // and based on the crdentials provider that was previously created.
            this.orgClient = new OrgClient(
                new OrgClientConfig { ServiceURI = new Uri(Config.OrgServiceUrl) },
                credentialsProvider);

            // Create the change set client used to handle large amounts of data.
            this.changeSetClient = new System.Net.Http.HttpClient();
        }

        /// <summary>
        /// The main method demonstrating the Organizer service .NET SDK capabilities.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        public async Task Run()
        {
            Console.WriteLine("----- Starting the Organizer service .NET SDK demo... -----");
            Console.WriteLine();

            // Generate a new forest ID to use in thes demo run
            string demoForestID = $"CDMServicesDemo:Org:Forest-{Guid.NewGuid().ToString()}";

            // Demonstrate tree related functionality
            await this.RunTreeDemo(demoForestID);

            // Demonstrate how to work with additional proeprties in requests and responses
            await this.RunAdditionalPropertiesDemo(demoForestID);

            // Demonstrate how to work with generic requests
            await this.RunGenericRequestsDemo(demoForestID);

            Console.WriteLine("----- Finished the Organizer service .NET SDK demo. -----");
            Console.WriteLine();
        }
    }
}
