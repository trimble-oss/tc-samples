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
        /// Gets all the libraries which belong to a specified project.
        /// </summary>
        /// <param name="projectId">The Id of the project.</param>
        /// <returns>The list of library IDs which belong to the specified project.</returns>
        private async Task<IList<string>> GetAllProjectLibraries(string projectId)
        {
            var allProjectLibraryIds = new List<string>();

            try
            {
                await ProcessAllProjectLibraries(projectId,
                (string libraryId) =>
                {
                    allProjectLibraryIds.Add(libraryId);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Handle the exception as required.
                Console.WriteLine(ex.Message);
            }

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
                NodeId = DiscoveryTreeLinksNodeID,
            };

            Console.WriteLine($"Getting the links of the project discovery tree links node (ProjectId={projectId}, ForestId={getNodeLinksRequest.ForestId}, TreeId={getNodeLinksRequest.TreeId}, NodeId={getNodeLinksRequest.NodeId})...");

            IList<string> discoveryTreeLinksNodeLinks = null;

            try
            {
                discoveryTreeLinksNodeLinks = await this.orgClient.GetNodeLinksAsync(getNodeLinksRequest, cancellationToken).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                // Handle the exception as required.
                Console.WriteLine(ex.Message);
            }

            if (discoveryTreeLinksNodeLinks != null)
            {
                Console.WriteLine($"Got {discoveryTreeLinksNodeLinks.Count} links:");
                discoveryTreeLinksNodeLinks.ToList().ForEach(link => Console.WriteLine(link));

                foreach (string link in discoveryTreeLinksNodeLinks)
                {
                    // Provide a chance to bail out before attempting to process the current link
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!link.StartsWith(DiscoveryTreeLinksNodeLinkPrefix))
                    {
                        throw new InvalidDataException("Project discovery tree links node link is in unexpected format.");
                    }

                    // The ID in the link may contain escaped special characters, so they must be un-escaped.
                    string libraryId = Uri.UnescapeDataString(link.Substring(DiscoveryTreeLinksNodeLinkPrefix.Length, link.Length - DiscoveryTreeLinksNodeLinkPrefix.Length));

                    libraryProcessor.Invoke(libraryId);
                }
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

                Library library = null;

                try
                {
                    library = await this.psetClient.GetLibraryAsync(getLibraryRequest).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    // Handle the exception as required.
                    Console.WriteLine(ex.Message);
                }

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