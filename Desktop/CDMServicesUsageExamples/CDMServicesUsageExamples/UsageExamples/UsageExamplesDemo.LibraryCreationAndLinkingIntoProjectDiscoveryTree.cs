//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.LibraryCreationAndLinkingIntoProjectDiscoveryTree.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using System;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Org.Client;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The usage examples demo.
    /// This part of the class demonstrates discovering PSet libraries.
    /// </summary>
    public partial class UsageExamplesDemo
    {
        /// <summary>
        /// Discover a library in a project by name.
        /// </summary>
        /// <param name="projectId">The ID of the project to which the library should be linked.</param>
        /// <param name="libraryName">The name of the library to create.</param>
        /// <returns>The created library.</returns>
        private async Task<Library> CreateLibraryAndLinkIntoProjectDiscoveryTree(string projectId, string libraryName)
        {
            Library library = await this.CreateLibrary(libraryName).ConfigureAwait(false);
            if (library != null)
            {
                string projectDiscoveryForestID = string.Format(ProjectDataForestIDPattern, projectId);
                string link = DiscoveryTreeLinksNodeLinkPrefix + Uri.EscapeDataString(library.Id);
                Node updatedProjectDiscoveryTreeLinksNode = null;
                try
                {
                    updatedProjectDiscoveryTreeLinksNode = await this.AddNodeLink(projectDiscoveryForestID, DiscoveryTreeID, DiscoveryTreeLinksNodeID, link).ConfigureAwait(false);
                }
                catch (InvalidServiceOperationException ex)
                {
                    // Handle the exception as required.
                    Console.WriteLine(ex.Message);

                    if (ex.ErrorCode == "NOT_FOUND" && ex.StatusCode == 404)
                    {
                        Console.WriteLine($"Could not find the project discovery tree's links node. Please create a PSet library on the Connect user interface first to ensure that the project discovery tree and its links node exist for the project {projectId}.");
                    }
                }
                catch (Exception ex)
                {
                    // Handle the exception as required.
                    Console.WriteLine(ex.Message);
                }

                if (updatedProjectDiscoveryTreeLinksNode != null)
                {
                    Console.WriteLine("The updated project discovery tree links node:");
                    this.PrintNode(updatedProjectDiscoveryTreeLinksNode);
                }
            }

            return library;
        }
    }
}