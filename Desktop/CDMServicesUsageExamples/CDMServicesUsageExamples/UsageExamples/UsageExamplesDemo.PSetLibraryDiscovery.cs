//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.PSetLibraryDiscovery.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Trimble.Connect.Org.Client;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The usage examples demo.
    /// This part of the class demonstrates discovering PSet libraries.
    /// </summary>
    public partial class UsageExamplesDemo
    {
        /// <summary>
        /// The pattern used to form the project data forest ID.
        /// </summary>
        private const string ProjectDataForestIDPattern = "project:{0}:data"; // Where {0} is the project ID

        /// <summary>
        /// The ID of the project discovery tree.
        /// </summary>
        private const string DiscoveryTreeID = "ProjectContext";

        /// <summary>
        /// The ID of the root node of the project discovery tree.
        /// </summary>
        private const string DiscoveryTreeRootNodeID = "PSetLibs";

        /// <summary>
        /// The prefix used in FRN links which contain library IDs.
        /// </summary>
        private const string DiscoveryTreeRootNodeLinkPrefix = "frn:lib:";

        /// <summary>
        /// Gets all the libraries which belong to a specified project.
        /// </summary>
        /// <param name="projectId">The Id of the project.</param>
        /// <returns>The list of library IDs which belong to the specified project.</returns>
        private async Task<IList<string>> GetAllProjectLibraries(string projectId)
        {
            var allProjectLibraryIds = new List<string>();

            await ProcessAllProjectLibraries(projectId,
            (string libraryId) =>
            {
                allProjectLibraryIds.Add(libraryId);
            }).ConfigureAwait(false);

            return allProjectLibraryIds;
        }

        /// <summary>
        /// Executes an action for all the libraries that belong to the specified project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="libraryProcessor">The action to execute for each library of the project.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        private async Task ProcessAllProjectLibraries(string projectId, Action<string> libraryProcessor, CancellationToken cancellationToken = default(CancellationToken))
        {
            var getNodeLinksRequest = new GetNodeLinksRequest
            {
                ForestId = string.Format(ProjectDataForestIDPattern, projectId),
                TreeId = DiscoveryTreeID,
                NodeId = DiscoveryTreeRootNodeID,
            };

            Console.WriteLine($"Getting the links of the project discovery tree root node (ProjectId={projectId}, ForestId={getNodeLinksRequest.ForestId}, TreeId={getNodeLinksRequest.TreeId}, NodeId={getNodeLinksRequest.NodeId})...");

            var discoveryTreeRootNodeLinks = await this.orgClient.GetNodeLinksAsync(getNodeLinksRequest, cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Got {discoveryTreeRootNodeLinks.Count} links:");
            discoveryTreeRootNodeLinks.ToList().ForEach(link => Console.WriteLine(link));

            foreach (string link in discoveryTreeRootNodeLinks)
            {
                // Provide a chance to bail out before attempting to process the current link
                cancellationToken.ThrowIfCancellationRequested();

                if (!link.StartsWith(DiscoveryTreeRootNodeLinkPrefix))
                {
                    throw new InvalidDataException("Project discovery tree root node link is in unexpected format.");
                }

                // The ID in the link may contain escaped special characters, so they must be un-escaped.
                string libraryId = Uri.UnescapeDataString(link.Substring(DiscoveryTreeRootNodeLinkPrefix.Length, link.Length - DiscoveryTreeRootNodeLinkPrefix.Length));

                libraryProcessor.Invoke(libraryId);
            }
        }

        /// <summary>
        /// Discover a library in a project by name.
        /// </summary>
        /// <param name="projectId">The ID of the project in which to discover the library.</param>
        /// <param name="libraryName">The name of the library to discover.</param>
        /// <returns>The ID of the discovered library.</returns>
        private async Task<string> DiscoverProjectLibrary(string projectId, string libraryName)
        {
            IList<string> projectLibraryIDs = await GetAllProjectLibraries(projectId).ConfigureAwait(false);
            foreach (string libraryID in projectLibraryIDs)
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

                if (library.Name == libraryName)
                {
                    Console.WriteLine("Library discovered!");

                    return library.Id;
                }
                else
                {
                    Console.WriteLine("Not a match.");
                }
            }

            return null;
        }
    }
}