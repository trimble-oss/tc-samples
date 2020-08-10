//-----------------------------------------------------------------------
// <copyright file="PSetDemo.GenericRequests.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates how to use generic requests to access functionality of the service
    /// which is not yet directly implemented in the .NET SDK.
    /// This makes it possible to immediately use the new functionality as it is released in the service,
    /// without the need to wait for it to also be implemented in the .NET SDK.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// Demonstrate using the generic requests.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        private async Task RunGenericRequestsDemo()
        {
            Library createdLibrary = await this.CreateLibraryUsingGenericRequest().ConfigureAwait(false);

            Library gotLibrary = await this.GetLibraryUsingGenericRequest(createdLibrary.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a library using a generic request, with explicitly specified arguments.
        /// </summary>
        /// <param name="libraryID">The ID of the library to get.</param>
        /// <returns>The library.</returns>
        private async Task<Library> GetLibraryUsingGenericRequest(string libraryID)
        {
            Console.WriteLine($"Getting library (using a generic request) with LibraryId={libraryID}...");

            Library library = await this.psetClient.SendAsync<JObject, Library>(
                $"libs/{libraryID}",
                null,
                HttpMethod.Get,
                new Dictionary<string, string> { { "deleted", "true" } }).ConfigureAwait(false);

            Console.Write($"Got library: ");
            this.PrintLibrary(library);
            Console.WriteLine();

            return library;
        }

        /// <summary>
        /// Tests creating a new library using a generic request, with default arguments.
        /// </summary>
        /// <returns>The created library.</returns>
        private async Task<Library> CreateLibraryUsingGenericRequest()
        {
            var requestObj = new JObject();
            requestObj.Add("Name", "Demo library " + Guid.NewGuid().ToString());
            requestObj.Add("Description", "Demo library");
            requestObj.Add("Type", "DemoLibrary");

            Console.WriteLine($"Creating library (using a generic request) with Name={requestObj["Name"].ToString()}, " +
                $"Description={requestObj["Description"].ToString()}...");

            Library library = await this.psetClient.SendAsync<JObject, Library>("libs", requestObj).ConfigureAwait(false);

            Console.Write($"Created library: ");
            this.PrintLibrary(library);
            Console.WriteLine();

            return library;
        }
    }
}