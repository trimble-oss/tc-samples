//-----------------------------------------------------------------------
// <copyright file="OrgDemo.Trees.cs" company="Trimble Inc.">
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
    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class demonstrates tree related operations.
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// Demonstrate tree related functionality.
        /// </summary>
        /// <param name="forestID">The Id of the forest in which to execute tree and node operations.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunTreeDemo(string forestID)
        {
            // Create two trees in the forest
            Tree createdTree1 = await this.CreateTree(forestID).ConfigureAwait(false);
            Tree createdTree2 = await this.CreateTree(forestID).ConfigureAwait(false);

            // Get the first created tree
            Tree gotTree = await this.GetTree(createdTree1.ForestId, createdTree1.Id).ConfigureAwait(false);

            // List all the trees in the forest
            await this.ListAllTrees(forestID).ConfigureAwait(false);

            // Do some node operations in the first tree
            await this.RunNodeDemo(createdTree1).ConfigureAwait(false);

            // Do some bulk node operations in the second tree
            await this.RunBulkOperationsDemo(createdTree2).ConfigureAwait(false);
        }

        /// <summary>
        /// Print the contents of a tree.
        /// </summary>
        /// <param name="tree">The tree to print.</param>
        private void PrintTree(Tree tree)
        {
            if (tree != null)
            {
                Console.WriteLine($"ForestId={tree.ForestId}, Id={tree.Id}, Name={tree.Name}, Description={tree.Description}, Type={tree.Type}, " +
                    $"CreatedAt={tree.CreatedAt}, ModifiedAt={tree.ModifiedAt}, Deleted={tree.Deleted == true}, Version={tree.Version}");
            }
        }

        /// <summary>
        /// Print the contents of a page of trees (multiple trees).
        /// </summary>
        /// <param name="trees">The page of trees to print.</param>
        private void PrintTrees(TreesPage trees)
        {
            if (trees != null && trees.Items != null)
            {
                this.PrintTrees(trees.Items);
            }
        }

        /// <summary>
        /// Print the contents of a collection of trees (multiple trees).
        /// </summary>
        /// <param name="trees">The trees to print.</param>
        private void PrintTrees(IEnumerable<Tree> trees)
        {
            if (trees != null)
            {
                trees.ToList().ForEach(tree => this.PrintTree(tree));
            }
        }

        /// <summary>
        /// Gets the specified tree.
        /// </summary>
        /// <param name="forestID">The forest ID of the tree.</param>
        /// <param name="treeID">The tree's ID.</param>
        /// <returns>The tree to get.</returns>
        private async Task<Tree> GetTree(string forestID, string treeID)
        {
            var getTreeRequest = new GetTreeRequest
            {
                ForestId = forestID,
                TreeId = treeID,
            };

            Console.WriteLine($"Getting tree with ForestId={getTreeRequest.ForestId}, TreeId={getTreeRequest.TreeId}...");

            Tree tree = await this.orgClient.GetTreeAsync(getTreeRequest).ConfigureAwait(false);

            Console.Write($"Got tree: ");
            this.PrintTree(tree);
            Console.WriteLine();

            return tree;
        }

        /// <summary>
        /// Lists all the trees in the specified forest.
        /// </summary>
        /// <param name="forestID">The ID of the forest.</param>
        /// <returns>Does not return anything.</returns>
        private async Task ListAllTrees(string forestID)
        {
            var listAllTreesRequest = new ListTreesRequest
            {
                ForestId = forestID,
            };

            Console.WriteLine("Listing all trees:");

            await this.orgClient.ListAllTreesAsync(listAllTreesRequest, this.PrintTrees).ConfigureAwait(false);

            Console.WriteLine();
        }

        /// <summary>
        /// Create a tree.
        /// </summary>
        /// <param name="forestID">The ID of the forest in which to create the tree.</param>
        /// <returns>The created tree.</returns>
        private async Task<Tree> CreateTree(string forestID)
        {
            var userID = await this.GetUserID().ConfigureAwait(false);

            var createTreeRequest = new CreateTreeRequest
            {
                ForestId = forestID,
                Id = $"CDMServicesDemo:Org:Tree-{Guid.NewGuid().ToString()}", // Optional (if not specified, the service will auto-generate it)
                Name = "DemoTree", // Optional
                Description = "A demo tree", // Optional
                Type = "DemoTree", // Optional
                Policy = new Policy // Optional
                {
                    Statements = new PolicyStatement[]
                    {
                        new PolicyStatement
                        {
                            Effect = "Allow",
                            Principal = new JValue($"user:{userID}"),
                            Action = new JValue("*"),
                            Resource = new JValue("*"),
                        },
                    },
                },
            };

            Console.WriteLine($"Creating tree with ForestId={createTreeRequest.ForestId}, Id={createTreeRequest.Id}, Name={createTreeRequest.Name}, Description={createTreeRequest.Description}, Type={createTreeRequest.Type}...");

            Tree tree = await this.orgClient.CreateTreeAsync(createTreeRequest).ConfigureAwait(false);

            Console.Write($"Created tree: ");
            this.PrintTree(tree);
            Console.WriteLine();

            return tree;
        }

        /// <summary>
        /// Obtains the ID of the user who is running the demo.
        /// </summary>
        /// <returns>The user's ID.</returns>
        private async Task<string> GetUserID()
        {
            var userClaims = await this.orgClient.GetMeAsync().ConfigureAwait(false);

            return userClaims.Sub;
        }
    }
}