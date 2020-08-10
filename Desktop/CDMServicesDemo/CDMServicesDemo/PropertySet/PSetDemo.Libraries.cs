//-----------------------------------------------------------------------
// <copyright file="PSetDemo.Libraries.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates library related operations.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// Demonstrate library related functionality.
        /// </summary>
        /// <returns>Does not return anything.</returns>
        private async Task RunLibraryDemo()
        {
            // Create two libraries
            Library createdLibrary1 = await this.CreateLibrary().ConfigureAwait(false);
            Library createdLibrary2 = await this.CreateLibrary().ConfigureAwait(false);

            // Get the first created library
            Library gotLibrary = await this.GetLibrary(createdLibrary1.Id).ConfigureAwait(false);

            // Do some definition operations in the first library
            await this.RunDefinitionDemo(createdLibrary1).ConfigureAwait(false);

            // Do some bulk property set operations in the second library
            //// await this.RunBulkOperationsDemo(createdLibrary2).ConfigureAwait(false);
        }

        /// <summary>
        /// Print the contents of a library.
        /// </summary>
        /// <param name="library">The library to print.</param>
        private void PrintLibrary(Library library)
        {
            if (library != null)
            {
                Console.WriteLine($"Id={library.Id}, Name={library.Name}, Description={library.Description}, " +
                    $"CreatedAt={library.CreatedAt}, ModifiedAt={library.ModifiedAt}, Deleted={library.Deleted == true}, Version={library.Version}");
            }
        }

        /// <summary>
        /// Print the contents of a collection of libraries (multiple libraries).
        /// </summary>
        /// <param name="libraries">The libraries to print.</param>
        private void PrintLibraries(IEnumerable<Library> libraries)
        {
            if (libraries != null)
            {
                libraries.ToList().ForEach(library => this.PrintLibrary(library));
            }
        }

        /// <summary>
        /// Gets the specified library.
        /// </summary>
        /// <param name="libraryID">The ID of the library.</param>
        /// <returns>The library to get.</returns>
        private async Task<Library> GetLibrary(string libraryID)
        {
            var getLibraryRequest = new GetLibraryRequest
            {
                LibraryId = libraryID,
            };

            Console.WriteLine($"Getting library with LibraryId={getLibraryRequest.LibraryId}...");

            Library library = await this.psetClient.GetLibraryAsync(getLibraryRequest).ConfigureAwait(false);

            Console.Write($"Got library: ");
            this.PrintLibrary(library);
            Console.WriteLine();

            return library;
        }

        /// <summary>
        /// Create a library.
        /// </summary>
        /// <returns>The created library.</returns>
        private async Task<Library> CreateLibrary()
        {
            // The optional policy can be constructed as a JSON string and then paresed into a Policy object.
            string libraryPolicyFullyPublic = @"{
                ""statements"": [
		            {
			            ""effect"": ""Allow"",
			            ""principal"": ""*"",
			            ""action"": ""*"",
			            ""resource"": ""*""
                    }
	            ]
            }";

            // Parse the policy JSON string into a Policy object
            Policy libraryPolicy = JsonConvert.DeserializeObject<Policy>(libraryPolicyFullyPublic);

            var createLibraryRequest = new CreateLibraryRequest
            {
                Id = $"CDMServicesDemo:PSet:Library-{Guid.NewGuid().ToString()}", // Optional (if not specified, the service will auto-generate it)
                Name = "DemoLibrary", // Optional
                Description = "A demo library", // Optional
                Policy = libraryPolicy, // Optional
            };

            Console.WriteLine($"Creating library with Id={createLibraryRequest.Id}, Name={createLibraryRequest.Name}, Description={createLibraryRequest.Description}...");

            Library library = await this.psetClient.CreateLibraryAsync(createLibraryRequest).ConfigureAwait(false);

            Console.Write($"Created library: ");
            this.PrintLibrary(library);
            Console.WriteLine();

            return library;
        }
    }
}