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
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;
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
        /// Discover a library in a project by name.
        /// </summary>
        /// <param name="projectId">The ID of the project in which to discover the library.</param>
        /// <param name="libraryName">The name of the library to discover.</param>
        /// <returns>The ID of the discovered library.</returns>
        private async Task<string> DiscoverLibrary(string projectId, string libraryName)
        {
            var getNodeLinksRequest = new GetNodeLinksRequest
            {
                ForestId = string.Format(ProjectDataForestIDPattern, projectId),
                TreeId = DiscoveryTreeID,
                NodeId = DiscoveryTreeRootNodeID,
            };

            Console.WriteLine($"Getting the links of the project discovery tree root node (ProjectId={projectId}, ForestId={getNodeLinksRequest.ForestId}, TreeId={getNodeLinksRequest.TreeId}, NodeId={getNodeLinksRequest.NodeId})...");

            var discoveryTreeRootNodeLinks = await this.orgClient.GetNodeLinksAsync(getNodeLinksRequest).ConfigureAwait(false);

            Console.WriteLine($"Got {discoveryTreeRootNodeLinks.Count} links:");
            discoveryTreeRootNodeLinks.ToList().ForEach(link => Console.WriteLine(link));

            foreach (string link in discoveryTreeRootNodeLinks)
            {
                if (!link.StartsWith(DiscoveryTreeRootNodeLinkPrefix))
                {
                    throw new InvalidDataException("Project discovery tree root node link is in unexpected format.");
                }

                var getLibraryRequest = new GetLibraryRequest
                {
                    LibraryId = link.Substring(DiscoveryTreeRootNodeLinkPrefix.Length, link.Length - DiscoveryTreeRootNodeLinkPrefix.Length),
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
    }
}