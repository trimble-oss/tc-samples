//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.ListPRopertySetsOfEntity.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The usage examples demo.
    /// This part of the class demonstrates listing all the property sets of an entity.
    /// </summary>
    public partial class UsageExamplesDemo
    {
        /// <summary>
        /// The format of the entity FRN links.
        /// </summary>
        private const string EntityLinkPattern = "frn:entity:{0}"; // Where {0} is the ID of the entity

        /// <summary>
        /// List all the property sets associated with a given entity within a given project.
        /// This approach requests all the property sets from the service which are associated with a link
        /// that is formed using the specified entity's ID and then filters the results by the library IDs
        /// which are associated with the specified project.
        /// An alternative approach would be to first request all the PSet definitions from all the project libraries
        /// and then iterate over the definitions and for each of them request all the PSets (using library ID, definition ID and link).
        /// Which approach is more optimal depends on the data which exists in the service.
        /// In the case when the specified entity exists in multiple projects and has many PSets associated with it,
        /// but in the specified project there are not many PSet definitions, the alternative approach might be more optimal.
        /// Otherwise, if there are many PSet definitions in the specified project overall
        /// (not just for the PSets associated with the specified entity), but the specified entity only exists in a few projects,
        /// the approach demonstrated here is more optimal.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="entityId">The ID of the entity.</param>
        private async Task ListPropertySetsOfEntityInProject(string projectId, string entityId)
        {
            // Get the list of the IDs of all the libraries which belong to the specified project.
            // We will use this list of library IDs to filter the property sets
            // because we only need the property sets which belong to the specified project (to libraries of the specified project).
            IList<string> projectLibraryIDs = await GetAllProjectLibraries(projectId).ConfigureAwait(false);

            // Get all the psets which are associated with a link that contains the specified entity's ID.
            var listPSetsOfLinkRequest = new ListPSetsOfLinkRequest
            {
                // FRN links may not contain special characters. See: https://drive.google.com/file/d/1gR1PEhrR58WGlrZt8PPWqTPbjvbEwXVK/view
                Link = string.Format(EntityLinkPattern, Uri.EscapeDataString(entityId)),
            };

            Console.WriteLine($"Listing all PSets associated with the link {listPSetsOfLinkRequest.Link}...");

            await this.psetClient.ListAllPSetsOfLinkAsync(listPSetsOfLinkRequest,
                (PSetsPage psetsPage) =>
                {
                    if (psetsPage != null && psetsPage.Items != null)
                    {
                        foreach (var pset in psetsPage.Items)
                        {
                            // Filter the returned PSets to keep only the ones which are in the libraries of the specified project.
                            // This is needed because the ListAllPSetsOfLinkAsync() call returns all the psets associated with the specified entity link,
                            // from all libraries, even if those libraries don't belong to the specified project.
                            if (projectLibraryIDs.Contains(pset.LibraryId))
                            {
                                PrintPSet(pset);
                            }
                            else
                            {
                                Console.WriteLine($"Filtering out PSet with LibraryId={pset.LibraryId}, DefinitionID={pset.DefinitionId}, Link={pset.Link} because it is not in the project {projectId}.");
                            }
                        }
                    }
                }).ConfigureAwait(false);
        }
    }
}