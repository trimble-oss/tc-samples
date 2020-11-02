//-----------------------------------------------------------------------
// <copyright file="PSetDemo.PSets.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates PSet (instance) related operations.
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// Demonstrate PSet related functionality.
        /// </summary>
        /// <param name="definition">The definition for which PSets will be created.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunPSetDemo(Definition definition)
        {
            if (definition != null)
            {
                // Create two PSets in the definition
                var props1 = new JObject();
                props1["str"] = "TestStringValue-1";
                props1["num"] = 101;
                PSet createdPSet1 = await this.CreatePSet(definition, props1).ConfigureAwait(false);

                var props2 = new JObject();
                props2["str"] = "TestStringValue-2";
                props2["num"] = 102;
                PSet createdPSet2 = await this.CreatePSet(definition, props2).ConfigureAwait(false);

                // Get the first created PSet
                PSet gotPSet = await this.GetPSet(createdPSet1.LibraryId, createdPSet1.DefinitionId, createdPSet1.Link).ConfigureAwait(false);

                // List all the PSets belongint to the definition
                await this.ListAllPSets(definition).ConfigureAwait(false);

                // Delete the first PSet
                PSet deletedPSet = await this.DeletePSet(createdPSet1).ConfigureAwait(false);

                // Update the deleted PSet's property values (the update will ressurrect the PSet)
                PSet updatedPSet = deletedPSet;
                updatedPSet.Props["str"] = updatedPSet.Props["str"] + "-UPDATED";
                updatedPSet.Props["num"] = (int)updatedPSet.Props["num"] + 100;
                updatedPSet = await this.UpdatePSet(updatedPSet).ConfigureAwait(false);

                // List all the versions of the updated PSet (there should be 3 versions: initial, deleted, updated)
                await this.ListAllPSetVersions(deletedPSet).ConfigureAwait(false);
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
        /// Print the contents of a page of PSets (multiple PSets).
        /// </summary>
        /// <param name="psets">The page of PSets to print.</param>
        private void PrintPSets(PSetsPage psets)
        {
            if (psets != null && psets.Items != null)
            {
                this.PrintPSets(psets.Items);
            }
        }

        /// <summary>
        /// Print the contents of a collection of PSets (multiple PSets).
        /// </summary>
        /// <param name="psets">The PSets to print.</param>
        private void PrintPSets(IEnumerable<PSet> psets)
        {
            if (psets != null)
            {
                psets.ToList().ForEach(PSet => this.PrintPSet(PSet));
            }
        }

        /// <summary>
        /// Gets the specified PSet.
        /// </summary>
        /// <param name="libraryId">The library ID of the PSet.</param>
        /// <param name="definitionID">The definition ID of the PSet.</param>
        /// <param name="link">The external link to which the PSet is linked.</param>
        /// <returns>The PSet to get.</returns>
        private async Task<PSet> GetPSet(string libraryId, string definitionID, string link)
        {
            var getPSetRequest = new GetPSetRequest
            {
                LibraryId = libraryId,
                DefinitionId = definitionID,
                Link = link,
            };

            Console.WriteLine($"Getting PSet with LibraryId={getPSetRequest.LibraryId}, DefinitionId={getPSetRequest.DefinitionId}, Link={getPSetRequest.Link}...");

            PSet pset = await this.psetClient.GetPSetAsync(getPSetRequest).ConfigureAwait(false);

            Console.Write($"Got PSet: ");
            this.PrintPSet(pset);
            Console.WriteLine();

            return pset;
        }

        /// <summary>
        /// Lists all the PSets to which the user has access to.
        /// </summary>
        /// <param name="definition">The definition for which to list PSets.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllPSets(Definition definition)
        {
            if (definition != null)
            {
                var listAllPSetsRequest = new ListPSetsRequest
                {
                    LibraryId = definition.LibraryId,
                    DefinitionId = definition.Id,
                };

                Console.WriteLine("Listing all PSets:");

                await this.psetClient.ListAllPSetsAsync(listAllPSetsRequest, this.PrintPSets).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Lists all the versions of the specified PSet.
        /// </summary>
        /// <param name="pset">The PSet for which to list versions.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllPSetVersions(PSet pset)
        {
            if (pset != null)
            {
                var listAllPSetVersionsRequest = new ListPSetVersionsRequest
                {
                    LibraryId = pset.LibraryId,
                    DefinitionId = pset.DefinitionId,
                    Link = pset.Link,
                };

                Console.WriteLine($"Listing all versions of the PSet with LibraryId={pset.LibraryId}, DefinitionId={pset.DefinitionId}, Link={pset.Link}:");

                await this.psetClient.ListAllPSetVersionsAsync(listAllPSetVersionsRequest, this.PrintPSets).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Create a PSet for a given definition.
        /// </summary>
        /// <param name="definition">The definition for which the PSet is created.</param>
        /// <param name="props">The properties of the PSet.</param>
        /// <returns>The created PSet.</returns>
        private async Task<PSet> CreatePSet(Definition definition, JObject props)
        {
            if (definition != null)
            {
                var createPSetRequest = new PutPSetRequest // In case of PSets the PutPSetRequest is used both for creation and for updating
                {
                    LibraryId = definition.LibraryId,
                    DefinitionId = definition.Id,
                    Link = $"CDMServicesDemo:PSet:PSet-{Guid.NewGuid().ToString()}",
                    Props = props,
                };

                Console.Write($"Creating PSet with LibraryId={createPSetRequest.LibraryId}, DefinitionId={createPSetRequest.DefinitionId}, Link={createPSetRequest.Link}, Props:");
                foreach (var prop in createPSetRequest.Props)
                {
                    Console.Write($" {prop.Key}={prop.Value}");
                }

                Console.WriteLine("...");

                PSet pset = await this.psetClient.PutPSetAsync(createPSetRequest).ConfigureAwait(false);

                Console.Write($"Created PSet: ");
                this.PrintPSet(pset);
                Console.WriteLine();

                return pset;
            }

            return null;
        }

        /// <summary>
        /// Update a PSet.
        /// </summary>
        /// <param name="updatedPSet">The updated PSet (which contains the updated properties).</param>
        /// <returns>The updated PSet.</returns>
        private async Task<PSet> UpdatePSet(PSet updatedPSet)
        {
            if (updatedPSet != null)
            {
                var updatePSetRequest = new PutPSetRequest // In case of PSets the PutPSetRequest is used both for creation and for updating
                {
                    LibraryId = updatedPSet.LibraryId,
                    DefinitionId = updatedPSet.DefinitionId,
                    Link = updatedPSet.Link,
                    Props = updatedPSet.Props,
                };

                Console.Write($"Updating PSet with LibraryId={updatePSetRequest.LibraryId}, DefinitionId={updatePSetRequest.DefinitionId}, Link={updatePSetRequest.Link}...");
                foreach (var prop in updatePSetRequest.Props)
                {
                    Console.Write($" {prop.Key}={prop.Value}");
                }

                Console.WriteLine("...");

                PSet pset = await this.psetClient.PutPSetAsync(updatePSetRequest).ConfigureAwait(false);

                Console.Write($"Updated PSet: ");
                this.PrintPSet(pset);
                Console.WriteLine();

                return pset;
            }

            return null;
        }

        /// <summary>
        /// Delete a PSet.
        /// </summary>
        /// <param name="pset">The PSet to delete.</param>        
        /// <returns>The deleted PSet.</returns>
        private async Task<PSet> DeletePSet(PSet pset)
        {
            if (pset != null)
            {
                var deletePSetRequest = new DeletePSetRequest
                {
                    LibraryId = pset.LibraryId,
                    DefinitionId = pset.DefinitionId,
                    Link = pset.Link,
                };

                Console.WriteLine($"Deleting PSet with LibraryId={deletePSetRequest.LibraryId}, DefinitionId={deletePSetRequest.DefinitionId}, Link={deletePSetRequest.Link}...");

                PSet deletedPSet = await this.psetClient.DeletePSetAsync(deletePSetRequest).ConfigureAwait(false);

                Console.Write($"Deleted PSet: ");
                this.PrintPSet(deletedPSet);
                Console.WriteLine();

                return deletedPSet;
            }

            return null;
        }
    }
}