//-----------------------------------------------------------------------
// <copyright file="PSetDemo.AdditionalProperties.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates how to use the <see cref="PSetModel.AdditionalProperties"/> property
    /// to launch requests and handle responses that are not yet "officially" supported in the .NET SDK.
    /// The <see cref="PSetModel.AdditionalProperties"/> property allows using request and response properties which
    /// are recognized by the service but not yet implemented in the .NET SDK.
    /// This makes it possible to immediately use the new functionality as it is released in the service,
    /// without the need to wait for it to also be implemented in the .NET SDK.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// Demonstrate using the AdditionalProperties property in requests and responses.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        private async Task RunAdditionalPropertiesDemo()
        {
            Library createdLibrary = await this.CreateLibraryUsingAdditionalProperties().ConfigureAwait(false);

            Library gotLibrary = await this.GetLibraryUsingAdditionalProperties(createdLibrary.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a library by using <see cref="PSetModel.AdditionalProperties"/> and <see cref="PSetRequest.AdditionalQueryParameters"/> in the request
        /// to mimic an existing request with a not yet supported one (in the .NET SDK)
        /// and interpret the response based on the <see cref="PSetModel.AdditionalProperties"/> .
        /// </summary>
        /// <param name="libraryID">The ID of the library to get.</param>
        /// <returns>The library.</returns>
        private async Task<Library> GetLibraryUsingAdditionalProperties(string libraryID)
        {
            var getLibraryRequest = new DemoGetLibraryRequest
            {
                // This is how the PSetModel.AdditionalProperties can be used to specify request properties
                // not yet supported by the .NET SDK (but already supported by the service).
                AdditionalProperties = new Dictionary<string, JToken>
                {
                    { "LibraryId", libraryID },
                },

                // This is how the PSetRequest.AdditionalQueryParameters to specify query parameters
                // not yet supported by the .NET SDK (but already supported by the service).
                AdditionalQueryParameters = new Dictionary<string, string> // Optional
                {
                    { "deleted", "true" },
                },
            };

            Console.WriteLine($"Getting library (using additional properties) with LibraryId={getLibraryRequest.AdditionalProperties["LibraryId"]}...");

            DemoEmptyPSetModel responseLibrary = await this.psetClient.SendAsync<DemoEmptyPSetModel>(getLibraryRequest).ConfigureAwait(false);

            Library library = new Library
            {
                Id = responseLibrary.AdditionalProperties["id"].ToString(),
                Name = responseLibrary.AdditionalProperties["name"].ToString(),
                Description = responseLibrary.AdditionalProperties["description"].ToString(),
                CreatedAt = (DateTime)responseLibrary.AdditionalProperties["createdAt"],
                ModifiedAt = (DateTime)responseLibrary.AdditionalProperties["modifiedAt"],
                CreatedBy = responseLibrary.AdditionalProperties["createdBy"].ToString(),
                ModifiedBy = responseLibrary.AdditionalProperties["modifiedBy"].ToString(),
                Deleted = responseLibrary.AdditionalProperties.ContainsKey("deleted"),
                Version = (int)responseLibrary.AdditionalProperties["v"],
            };

            Console.Write("Got library: ");
            this.PrintLibrary(library);
            Console.WriteLine();

            return library;
        }

        /// <summary>
        /// Create a new library by using additional properties in the request.
        /// </summary>
        /// <returns>The created library.</returns>
        private async Task<Library> CreateLibraryUsingAdditionalProperties()
        {
            var createLibraryRequest = new CreateLibraryRequest
            {
                AdditionalProperties = new Dictionary<string, JToken>
                {
                    { "Name", "Demo library " },
                    { "Description", "Demo library" },
                },
            };

            Console.WriteLine($"Creating library (using additional properties) with Name={createLibraryRequest.AdditionalProperties["Name"]}, " +
                $"Description={createLibraryRequest.AdditionalProperties["Description"]}...");

            Library createdLibrary = await this.psetClient.CreateLibraryAsync(createLibraryRequest).ConfigureAwait(false);

            Console.Write("Created library: ");
            this.PrintLibrary(createdLibrary);
            Console.WriteLine();

            return createdLibrary;
        }

        /// <summary>
        /// A generic derived class of PSetModel, with no properties (basically empty),
        /// used for demonstrating the usage of the <see cref="PSetModel.AdditionalProperties"/> property in responses.
        /// It is used to mimic an existing concrete derived class of <see cref="PSetModel"/>, such as <see cref="Library"/>.
        /// In the same way it is possible to emulate a response class that is already supported by the service,
        /// but not yet supported by the .NET SDK.
        /// </summary>
        private class DemoEmptyPSetModel : PSetModel
        {
        }

        /// <summary>
        /// An artificial version of the <see cref="GetLibraryRequest"/> used for demonstrating the usage of <see cref="PSetModel.AdditionalProperties"/> in requests.
        /// Just as the <see cref="PSetModel.AdditionalProperties"/> property is used in this request class to refer to currently supported request properties (in the .NET SDK),
        /// it can also be used to refer to request properties which may be already supported by the service but not yet supported by the .NET SDK.
        /// </summary>
        private class DemoGetLibraryRequest : PSetRequest
        {
            /// <summary>
            /// Demonstrates how to override the request URL.
            /// </summary>
            [JsonIgnore]
            public override string URI => $"libs/{this.AdditionalProperties["LibraryId"]}";

            /// <summary>
            /// Demonstrates how to override the request method.
            /// </summary>
            [JsonIgnore]
            public override HttpMethod Method => HttpMethod.Get;

            /// <summary>
            /// Demonstrates how to override the request validation.
            /// </summary>
            public override void Validate()
            {
                base.Validate();

                this.ThrowIfPropertyIsNull(this.AdditionalProperties["LibraryId"], "LibraryId");
            }
        }
    }
}