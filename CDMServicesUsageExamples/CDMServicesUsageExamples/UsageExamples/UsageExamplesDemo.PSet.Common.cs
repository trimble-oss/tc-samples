//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.PSet.Common.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using Trimble.Connect.Org.Client;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The usage examples demo.
    /// This part of the class contains PSet-specific common functionality.
    /// </summary>
    public partial class UsageExamplesDemo
    {
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
        /// Print the contents of a PSet.
        /// </summary>
        /// <param name="pset">The PSet to print.</param>
        private void PrintPSet(PSet pset)
        {
            if (pset != null)
            {
                Console.Write($"LibraryId={pset.LibraryId}, DefinitionId={pset.DefinitionId}, Link={pset.Link}," +
                    $"CreatedAt={pset.CreatedAt}, ModifiedAt={pset.ModifiedAt}, Deleted={pset.Deleted == true}, Version={pset.Version}, SchemaVersion={pset.SchemaVersion}");

                if (pset.Props != null)
                {
                    Console.Write(" Props:");
                    foreach (var prop in pset.Props)
                    {
                        Console.Write($" {prop.Key}={prop.Value}");
                    }
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Create a PSet library.
        /// </summary>
        /// <param name="libraryName">The name of the library to be created.</param>
        /// <returns>The created library.</returns>
        private async Task<Library> CreateLibrary(string libraryName)
        {
            var userID = await this.GetUserID().ConfigureAwait(false);

            var createLibraryRequest = new CreateLibraryRequest
            {
                Id = $"CDMServicesDemo:PSet:Library-{Guid.NewGuid().ToString()}",
                Name = libraryName,
                Description = "A demo library",
                Policy = new Trimble.Connect.PSet.Client.Policy
                {
                    Statements = new Trimble.Connect.PSet.Client.PolicyStatement[]
                    {
                        new Trimble.Connect.PSet.Client.PolicyStatement
                        {
                            Effect = "Allow",
                            Principal = new JValue($"user:{userID}"),
                            Action = new JValue("*"),
                            Resource = new JValue("*"),
                        },
                    },
                },
            };

            Console.WriteLine($"Creating library with Id={createLibraryRequest.Id}, Name={createLibraryRequest.Name}, Description={createLibraryRequest.Description}...");

            Library library = null;

            try
            {
                library = await this.psetClient.CreateLibraryAsync(createLibraryRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Handle the exception as required.
                Console.WriteLine(ex.Message);
            }

            if (library != null)
            {
                Console.Write($"Created library: ");
                this.PrintLibrary(library);
                Console.WriteLine();
            }

            return library;
        }
    }
}