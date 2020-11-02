//-----------------------------------------------------------------------
// <copyright file="PSetDemo.Definitions.cs" company="Trimble Inc.">
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
    using Newtonsoft.Json.Linq;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates definition related operations.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// Demonstrate definition related functionality.
        /// </summary>
        /// <param name="library">The library in which to execute definition operations.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunDefinitionDemo(Library library)
        {
            // Create two definitions in the library
            Definition createdDefinition1 = await this.CreateDefinition(library).ConfigureAwait(false);
            Definition createdDefinition2 = await this.CreateDefinition(library).ConfigureAwait(false);

            // Get the first created definition
            Definition gotDefinition = await this.GetDefinition(createdDefinition1.LibraryId, createdDefinition1.Id).ConfigureAwait(false);

            // List all the definitions in the library
            await this.ListAllDefinitions(library).ConfigureAwait(false);

            // Do some PSet operations in the first definition
            await this.RunPSetDemo(createdDefinition1).ConfigureAwait(false);

            // Do some bulk PSet operations in the second definition
            await this.RunBulkOperationsDemo(createdDefinition2).ConfigureAwait(false);
        }

        /// <summary>
        /// Print the contents of a definition.
        /// </summary>
        /// <param name="definition">The definition to print.</param>
        private void PrintDefinition(Definition definition)
        {
            if (definition != null)
            {
                Console.Write($"LibraryId={definition.LibraryId}, Id={definition.Id}, Name={definition.Name}, Description={definition.Description}, " +
                    $"CreatedAt={definition.CreatedAt}, ModifiedAt={definition.ModifiedAt}, Deleted={definition.Deleted == true}, Version={definition.Version}");

                if (definition.Types != null)
                {
                    Console.Write(", Types:");
                    foreach (var defType in definition.Types)
                    {
                        Console.Write($" {defType}");
                    }
                }

                if (definition.Schema != null)
                {
                    Console.Write($", SchemaVersion: {definition.Schema.Version}");

                    if (definition.Schema.Properties != null)
                    {
                        Console.Write($", SchemaProperties:");
                        foreach (JProperty prop in definition.Schema.Properties.Children())
                        {
                            Console.Write($" {prop.Name}({prop.Value["type"]})({prop.Value["description"]})");
                        }
                    }
                }

                //// For the sake of simplicity we won't print the definition.I18N property.

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print the contents of a page of definitions (multiple definitions).
        /// </summary>
        /// <param name="definitions">The page of definitions to print.</param>
        private void PrintDefinitions(DefinitionsPage definitions)
        {
            if (definitions != null && definitions.Items != null)
            {
                this.PrintDefinitions(definitions.Items);
            }
        }

        /// <summary>
        /// Print the contents of a collection of definitions (multiple definitions).
        /// </summary>
        /// <param name="definitions">The definitions to print.</param>
        private void PrintDefinitions(IEnumerable<Definition> definitions)
        {
            if (definitions != null)
            {
                definitions.ToList().ForEach(definition => this.PrintDefinition(definition));
            }
        }

        /// <summary>
        /// Gets the specified definition.
        /// </summary>
        /// <param name="libraryID">The library ID of the definition.</param>
        /// <param name="definitionID">The definition's ID.</param>
        /// <returns>The definition to get.</returns>
        private async Task<Definition> GetDefinition(string libraryID, string definitionID)
        {
            var getDefinitionRequest = new GetDefinitionRequest
            {
                LibraryId = libraryID,
                DefinitionId = definitionID,
            };

            Console.WriteLine($"Getting definition with LibraryId={getDefinitionRequest.LibraryId}, DefinitionId={getDefinitionRequest.DefinitionId}...");

            Definition definition = await this.psetClient.GetDefinitionAsync(getDefinitionRequest).ConfigureAwait(false);

            Console.Write($"Got definition: ");
            this.PrintDefinition(definition);
            Console.WriteLine();

            return definition;
        }

        /// <summary>
        /// Lists al the definitions in the specified library.
        /// </summary>
        /// <param name="library">The library from which to list definitions.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllDefinitions(Library library)
        {
            var listAllDefinitionsRequest = new ListDefinitionsRequest
            {
                LibraryId = library.Id,
            };

            Console.WriteLine("Listing all definitions:");

            await this.psetClient.ListAllDefinitionsAsync(listAllDefinitionsRequest, this.PrintDefinitions).ConfigureAwait(false);

            Console.WriteLine();
        }

        /// <summary>
        /// Create a definition.
        /// </summary>
        /// <param name="library">The library in which to create the definition.</param>
        /// <returns>The created definition.</returns>
        private async Task<Definition> CreateDefinition(Library library)
        {
            // The schema properties can be constructed as a JSON string and then paresed into a JObject object.
            string demoPSetDefSchemaPropsStr = @"
            {
                ""str"":
                {
                    ""type"": ""string"",
                    ""description"": ""A string.""
                },
                ""num"":
                {
                    ""type"": ""integer"",
                    ""description"": ""A number.""
                }
            }";

            var demoPSetDefSchema = new SchemaRequest
            {
                Open = false,
                Properties = JObject.Parse(demoPSetDefSchemaPropsStr), // Parse the schema properties JSON string into a JObject object
            };

            // The optional I18N can be constructed as a JSON string and then paresed into a JObject object.
            string testPSetDefSchemaI18NStr = @"
            {
	            ""en"": {
                    ""name"": ""Test PSet"",
		            ""description"": ""This is a test PSet"",
		            ""props"": {
                        ""str"": ""String property."",
			            ""num"": ""Numeric property.""
                    }
                }
            }";

            var createDefinitionRequest = new CreateDefinitionRequest
            {
                LibraryId = library.Id,
                Id = $"CDMServicesDemo:PSet:Definition-{Guid.NewGuid().ToString()}", // Optional (if not specified, the service will auto-generate it)
                Name = "DemoDefinition", // Optional
                Description = "A demo definition", // Optional
                Schema = demoPSetDefSchema,
                I18N = JObject.Parse(testPSetDefSchemaI18NStr), // Optional. Parse the I18N JSON string into a JObject object
                Types = new string[] { "Wall", "Slab" },
            };

            Console.WriteLine($"Creating definition with LibraryId={createDefinitionRequest.LibraryId}, Id={createDefinitionRequest.Id}" +
                $", Name={createDefinitionRequest.Name}, Description={createDefinitionRequest.Description}...");

            Definition definition = await this.psetClient.CreateDefinitionAsync(createDefinitionRequest).ConfigureAwait(false);

            Console.Write($"Created definition: ");
            this.PrintDefinition(definition);
            Console.WriteLine();

            return definition;
        }
    }
}