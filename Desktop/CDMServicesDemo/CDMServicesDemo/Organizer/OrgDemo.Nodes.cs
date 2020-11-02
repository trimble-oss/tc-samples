//-----------------------------------------------------------------------
// <copyright file="OrgDemo.Nodes.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class demonstrates node related operations.
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// Demonstrate node related functionality.
        /// </summary>
        /// <param name="parentTree">The parent tree in which nodes will be created.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunNodeDemo(Tree parentTree)
        {
            if (parentTree != null)
            {
                // Create two nodes in the tree
                Node createdNode1 = await this.CreateNode(parentTree).ConfigureAwait(false);
                Node createdNode2 = await this.CreateNode(parentTree).ConfigureAwait(false);

                // Get the first created node
                Node gotNode = await this.GetNode(createdNode1.ForestId, createdNode1.TreeId, createdNode1.Id).ConfigureAwait(false);

                // List all the nodes in the tree
                await this.ListAllNodes(parentTree).ConfigureAwait(false);

                // Delete the first node
                Node deletedNode = await this.DeleteNode(createdNode1).ConfigureAwait(false);

                // Update the deleted node with a modified name and one additional link (the update will ressurrect the node)
                Node updatedNode = deletedNode;
                updatedNode.Name = updatedNode.Name + "-UPDATED";
                updatedNode.Links.Add("frn:DemoLink-03");
                updatedNode = await this.UpdateNode(updatedNode).ConfigureAwait(false);

                // List all the versions of the updated node (there should be 3 versions: initial, deleted, updated)
                await this.ListAllNodeVersions(deletedNode).ConfigureAwait(false);

                // Search for nodes in the forest by link value
                await this.SearchNodesInForest(parentTree.ForestId, "frn:DemoLink-01", true).ConfigureAwait(false);

                // Search for nodes in the tree by link value
                await this.SearchNodesInTree(parentTree, "frn:DemoLink-01", true).ConfigureAwait(false);
                await this.SearchNodesInTree(parentTree, "frn:DemoLink-03", true).ConfigureAwait(false);
                await this.SearchNodesInTree(parentTree, "InexistentLinkValue", true).ConfigureAwait(false);                
            }
        }

        /// <summary>
        /// Print the contents of a node.
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
        /// Print the contents of a page of nodes (multiple nodes).
        /// </summary>
        /// <param name="nodes">The page of nodes to print.</param>
        private void PrintNodes(NodesPage nodes)
        {
            if (nodes != null && nodes.Items != null)
            {
                this.PrintNodes(nodes.Items);
            }
        }

        /// <summary>
        /// Print the contents of a collection of nodes (multiple nodes).
        /// </summary>
        /// <param name="nodes">The nodes to print.</param>
        private void PrintNodes(IEnumerable<Node> nodes)
        {
            if (nodes != null)
            {
                nodes.ToList().ForEach(node => this.PrintNode(node));
            }
        }

        /// <summary>
        /// Gets the specified node.
        /// </summary>
        /// <param name="forestID">The forest ID of the node.</param>
        /// <param name="treeID">The tree ID of the node.</param>
        /// <param name="nodeID">The node's ID.</param>
        /// <returns>The node to get.</returns>
        private async Task<Node> GetNode(string forestID, string treeID, string nodeID)
        {
            var getNodeRequest = new GetNodeRequest
            {
                ForestId = forestID,
                TreeId = treeID,
                NodeId = nodeID,
            };

            Console.WriteLine($"Getting node with ForestId={getNodeRequest.ForestId}, TreeId={getNodeRequest.TreeId}, NodeId={getNodeRequest.NodeId}...");

            Node node = await this.orgClient.GetNodeAsync(getNodeRequest).ConfigureAwait(false);

            Console.Write($"Got node: ");
            this.PrintNode(node);
            Console.WriteLine();

            return node;
        }

        /// <summary>
        /// Lists al the nodes in the specified tree.
        /// </summary>
        /// <param name="parentTree">The tree from which to list nodes.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllNodes(Tree parentTree)
        {
            if (parentTree != null)
            {
                var listAllNodesRequest = new ListNodesRequest
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                };

                Console.WriteLine("Listing all nodes:");

                await this.orgClient.ListAllNodesAsync(listAllNodesRequest, this.PrintNodes).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Lists all the versions of the specified node.
        /// </summary>
        /// <param name="node">The node for which to list versions.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllNodeVersions(Node node)
        {
            if (node != null)
            {
                var listAllNodeVersionsRequest = new ListNodeVersionsRequest
                {
                    ForestId = node.ForestId,
                    TreeId = node.TreeId,
                    NodeId = node.Id,
                };

                Console.WriteLine($"Listing all versions of the node with ForestId={node.ForestId}, TreeId={node.TreeId}, NodeId={node.Id}:");

                await this.orgClient.ListAllNodeVersionsAsync(listAllNodeVersionsRequest, this.PrintNodes).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Search for nodes in the specified forest by link value.
        /// </summary>
        /// <param name="forestID">The forest in which to search for nodes.</param>
        /// <param name="link">the link value to search for.</param>
        /// <param name="returnLinks">Should link values be returned in the results?</param>
        /// <returns>Does not return anything.</returns>
        private async Task SearchNodesInForest(string forestID, string link, bool returnLinks = false)
        {
            if (!string.IsNullOrEmpty(forestID))
            {
                var searchNodesRequest = new SearchNodesRequest
                {
                    ForestId = forestID,
                    Link = link,
                    NoLinks = !returnLinks,
                };

                Console.WriteLine($"Search nodes for link {link} in forest {forestID} results:");

                await this.orgClient.SearchAllNodesAsync(searchNodesRequest, this.PrintNodes).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Search for nodes in the specified tree by link value.
        /// </summary>
        /// <param name="parentTree">The tree in which to search for nodes.</param>
        /// <param name="link">the link value to search for.</param>
        /// <param name="returnLinks">Should link values be returned in the results?</param>
        /// <returns>Does not return anything.</returns>
        private async Task SearchNodesInTree(Tree parentTree, string link, bool returnLinks = false)
        {
            if (parentTree != null)
            {
                var searchNodesRequest = new SearchNodesRequest
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                    Link = link,
                    NoLinks = !returnLinks,
                };

                Console.WriteLine($"Search nodes for link {link} in tree {parentTree.ForestId}/{parentTree.Id} results:");

                await this.orgClient.SearchAllNodesAsync(searchNodesRequest, this.PrintNodes).ConfigureAwait(false);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Create a node in a tree.
        /// </summary>
        /// <param name="parentTree">The parent tree in which the node is created.</param>
        /// <returns>The created node.</returns>
        private async Task<Node> CreateNode(Tree parentTree)
        {
            if (parentTree != null)
            {
                var createNodeRequest = new CreateNodeRequest
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                    Id = $"CDMServicesDemo:Org:Node-{Guid.NewGuid().ToString()}", // Optional (if not specified, the service will auto-generate it)
                    Name = "DemoNode", // Optional
                    Links = new string[] // Optional
                    {
                        "frn:DemoLink-01",
                        "frn:DemoLink-02"
                    },
                };

                Console.WriteLine($"Creating node with ForestId={createNodeRequest.ForestId}, TreeId={createNodeRequest.TreeId}, Id={createNodeRequest.Id}, Name={createNodeRequest.Name}...");

                Node node = await this.orgClient.CreateNodeAsync(createNodeRequest).ConfigureAwait(false);

                Console.Write($"Created node: ");
                this.PrintNode(node);
                Console.WriteLine();

                return node;
            }

            return null;
        }

        /// <summary>
        /// Update a node.
        /// </summary>
        /// <param name="updatedNode">The updated node (which contains the updated properties).</param>
        /// <returns>The updated node.</returns>
        private async Task<Node> UpdateNode(Node updatedNode)
        {
            if (updatedNode != null)
            {
                var updateNodeRequest = new UpsertNodeRequest
                {
                    ForestId = updatedNode.ForestId,
                    TreeId = updatedNode.TreeId,
                    NodeId = updatedNode.Id,
                    Name = updatedNode.Name, // Optional
                    Links = updatedNode.Links, // Optional
                };

                Console.Write($"Updating node with ForestId={updateNodeRequest.ForestId}, TreeId={updateNodeRequest.TreeId}, NodeId={updateNodeRequest.NodeId}, Name={updateNodeRequest.Name}");
                if (updatedNode.Links != null)
                {
                    foreach (var link in updatedNode.Links)
                    {
                        Console.Write(" {link}");
                    }
                }

                Console.WriteLine("...");

                Node node = await this.orgClient.UpdateNodeAsync(updateNodeRequest).ConfigureAwait(false);

                Console.Write($"Updated node: ");
                this.PrintNode(node);
                Console.WriteLine();

                return node;
            }

            return null;
        }

        /// <summary>
        /// Delete a node.
        /// </summary>
        /// <param name="node">The node to delete.</param>        
        /// <returns>The deleted node.</returns>
        private async Task<Node> DeleteNode(Node node)
        {
            if (node != null)
            {
                var deleteNodeRequest = new DeleteNodeRequest
                {
                    ForestId = node.ForestId,
                    TreeId = node.TreeId,
                    NodeId = node.Id,
                };

                Console.WriteLine($"Deleting node with ForestId={deleteNodeRequest.ForestId}, TreeId={deleteNodeRequest.TreeId}, NodeId={deleteNodeRequest.NodeId}...");

                Node deletedNode = await this.orgClient.DeleteNodeAsync(deleteNodeRequest).ConfigureAwait(false);

                Console.Write($"Deleted node: ");
                this.PrintNode(deletedNode);
                Console.WriteLine();

                return deletedNode;
            }

            return null;
        }
    }
}