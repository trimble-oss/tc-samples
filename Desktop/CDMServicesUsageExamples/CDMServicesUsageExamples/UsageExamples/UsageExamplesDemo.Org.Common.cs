//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.Org.Common.cs" company="Trimble Inc.">
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
    /// This part of the class contains Organizer-specific common functionality.
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
        /// The ID of the links node of the project discovery tree.
        /// </summary>
        private const string DiscoveryTreeLinksNodeID = "PSetLibs";

        /// <summary>
        /// The prefix used in FRN links which contain library IDs.
        /// </summary>
        private const string DiscoveryTreeLinksNodeLinkPrefix = "frn:lib:";

        /// <summary>
        /// Print the contents of an Organizer node.
        /// </summary>
        /// <param name="node">The node to print.</param>
        private void PrintNode(Node node)
        {
            if (node != null)
            {
                Console.Write($"ForestId={node.ForestId}, TreeId={node.TreeId}, Id={node.Id}, Name={node.Name}," +
                    $"CreatedAt={node.CreatedAt}, ModifiedAt={node.ModifiedAt}, Deleted={node.Deleted == true}, Version={node.Version} Links:");

                if (node.Links != null)
                {
                    foreach (var link in node.Links)
                    {
                        Console.Write($" {link}");
                    }
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Add a link to an Organizer node.
        /// </summary>
        /// <param name="forestID">The forest ID of the node.</param>
        /// <param name="treeID">The tree ID of the node.</param>
        /// <param name="nodeID">The node's ID.</param>
        /// <param name="link">The link to add.</param>
        /// <returns>The updated node.</returns>
        private async Task<Node> AddNodeLink(string forestID, string treeID, string nodeID, string link)
        {
            var updateNodeRequest = new UpsertNodeRequest
            {
                ForestId = forestID,
                TreeId = treeID,
                NodeId = nodeID,
                Add = new string[] { link },
            };

            Console.Write($"Updating node with ForestId={updateNodeRequest.ForestId}, TreeId={updateNodeRequest.TreeId}, NodeId={updateNodeRequest.NodeId}, Name={updateNodeRequest.Name}...");

            Node updatedNode = null;

            try
            {
                updatedNode = await this.orgClient.UpdateNodeAsync(updateNodeRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Handle the exception as required.
                Console.WriteLine(ex.Message);
            }

            if (updatedNode != null)
            {
                Console.Write($"Updated node: ");
                this.PrintNode(updatedNode);
                Console.WriteLine();
            }

            return updatedNode;
        }
    }
}