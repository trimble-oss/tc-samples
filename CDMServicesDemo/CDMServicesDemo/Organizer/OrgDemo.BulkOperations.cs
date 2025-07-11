//-----------------------------------------------------------------------
// <copyright file="OrgDemo.BulkOperations.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class demonstrates bulk operations that handle large amounts of data (change sets and batch gets).
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// How many nodes to have in the change set
        /// </summary>
        private const int ChangeSetNodeCount = 25;

        /// <summary>
        /// How many links to have in each node
        /// </summary>
        private const int ChangeSetNodeLinkCount = 5;

        /// <summary>
        /// The ID pattern to be used in change set nodes
        /// </summary>
        private const string ChangeSetNodeIDPattern = "ChangeSetNode_{0}"; // Where {0} is the index of the node

        /// <summary>
        /// The change set client used to handle large amounts of data.
        /// </summary>
        private HttpClient changeSetClient = null;

        /// <summary>
        /// Demonstrate change set and batch get functionality.
        /// </summary>
        /// <param name="parentTree">The parent tree in which nodes will be created.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunBulkOperationsDemo(Tree parentTree)
        {
            // Asynchronous change set demo
            await this.RunAsyncChangeSetWorkflow(parentTree).ConfigureAwait(false);

            // Batch-get multiple trees and nodes
            TreeIdentity[] trees = new TreeIdentity[]
            {
                new TreeIdentity
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                },
                new TreeIdentity
                {
                    ForestId = parentTree.ForestId,
                    TreeId = Guid.NewGuid().ToString(), // Inexistent, will not be found
                },
            };

            TreeNodeIdentity[] nodes = new TreeNodeIdentity[ChangeSetNodeCount];
            for (int nodeIdx = 0; nodeIdx < ChangeSetNodeCount; ++nodeIdx)
            {
                nodes[nodeIdx] = new TreeNodeIdentity
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                    NodeId = string.Format(ChangeSetNodeIDPattern, nodeIdx),
                };
            }

            await this.BatchGet(trees, nodes).ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrate the full asynchronous change set workflow (create change set, upload contents, check results).
        /// </summary>
        /// <param name="parentTree">The tree to which to apply the change set.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunAsyncChangeSetWorkflow(Tree parentTree)
        {
            if (parentTree != null)
            {
                // Create an asynchronous change set (declare intention to upload changes)
                ChangeSetResponseInitiated createAsyncChangesSetResponse = await this.CreateAsyncChangeset(parentTree).ConfigureAwait(false);

                if (string.Compare(createAsyncChangesSetResponse.Status, "WaitingUpload", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Upload the change set contents asynchronously
                    await this.UploadChangeSetContents(createAsyncChangesSetResponse.UploadURL).ConfigureAwait(false);

                    // Wait until the change set has finished processing.
                    // Use a change set status checking algorithm with exponential backoff an jitter
                    // to reduce the load on the server and increase chance of success.
                    const int CheckhangeSetStatusInitialInterval = 2000;
                    const int CheckChangeSetstatusMaxInterval = 30000;
                    const double CheckChangeSetIntervalIncreaseFactor = 2.0;
                    const int CheckChangeSetMaxJitter = 1000;
                    const int ChangeSetTimeOut = 90000;

                    int checkChangeSetStatusInterval = CheckhangeSetStatusInitialInterval;
                    Random random = new Random();
                    ChangeSetResponse getChangeSetStatusResponse = null;
                    Stopwatch watch = Stopwatch.StartNew();

                    do
                    {
                        Console.WriteLine($"Waiting {(int)checkChangeSetStatusInterval/1000.0} sec...");
                        Thread.Sleep(checkChangeSetStatusInterval);

                        getChangeSetStatusResponse = await this.GetChangeSetStatus(createAsyncChangesSetResponse.Id).ConfigureAwait(false);

                        checkChangeSetStatusInterval = Math.Min((int)(checkChangeSetStatusInterval * CheckChangeSetIntervalIncreaseFactor), CheckChangeSetstatusMaxInterval) + random.Next(CheckChangeSetMaxJitter);
                    }
                    while ((string.Compare(getChangeSetStatusResponse.Status, "WaitingUpload", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(getChangeSetStatusResponse.Status, "Queued", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(getChangeSetStatusResponse.Status, "Processing", StringComparison.OrdinalIgnoreCase) == 0) &&
                        watch.ElapsedMilliseconds <= ChangeSetTimeOut);

                    Console.WriteLine($"Change set status: {getChangeSetStatusResponse.Status}");

                    if (string.Compare(getChangeSetStatusResponse.Status, "Done", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // Get the change set results
                        if (!string.IsNullOrEmpty(getChangeSetStatusResponse.ResultsURL))
                        {
                            Console.WriteLine("Change set results: ");
                            await this.changeSetClient.GetNDJsonAsync<Node>(getChangeSetStatusResponse.ResultsURL, this.PrintNodes).ConfigureAwait(false);
                        }

                        // Get the change set errors
                        if (!string.IsNullOrEmpty(getChangeSetStatusResponse.ErrorsURL))
                        {
                            Console.WriteLine("Change set errors: ");
                            await this.changeSetClient.GetNDJsonAsync(
                                getChangeSetStatusResponse.ErrorsURL,
                                (IEnumerable<UnprocessedNode> unprocessedNodesChunk) =>
                                {
                                    if (unprocessedNodesChunk != null)
                                    {
                                        foreach (var unprocessedNode in unprocessedNodesChunk)
                                        {
                                            Console.Write($" {unprocessedNode.Code} {unprocessedNode.Message}");
                                            if (unprocessedNode.Item != null)
                                            {
                                                Console.Write($" {unprocessedNode.Item.Id} {unprocessedNode.Item.Name}");
                                            }
                                        }

                                        Console.WriteLine();
                                    }
                                }).ConfigureAwait(false);
                        }
                    }
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Retrieve the status of a change set.
        /// </summary>
        /// <param name="changeSetID">The Id of the change set.</param>
        /// <returns>The change set status response.</returns>
        private async Task<ChangeSetResponse> GetChangeSetStatus(string changeSetID)
        {
            var getChangeSetStatusRequest = new GetChangeSetStatusRequest
            {
                ChangeSetId = changeSetID,
            };

            Console.Write($"Getting status of change set {changeSetID}... ");

            ChangeSetResponse changeSetStatusResponse = await this.orgClient.GetChangeSetStatusAsync(getChangeSetStatusRequest).ConfigureAwait(false);

            Console.WriteLine(changeSetStatusResponse.Status);

            return changeSetStatusResponse;
        }

        /// <summary>
        /// Create an asynchronous change set.
        /// </summary>
        /// <param name="parentTree">The tree for which to create the change set.</param>
        /// <returns>The change set creation response.</returns>
        private async Task<ChangeSetResponseInitiated> CreateAsyncChangeset(Tree parentTree)
        {
            if (parentTree != null)
            {
                var createChangeSetAsyncRequest = new CreateChangeSetAsyncRequest
                {
                    ForestId = parentTree.ForestId,
                    TreeId = parentTree.Id,
                };

                Console.WriteLine("Creating asynchronous change set...");

                return await this.orgClient.CreateChangeSetAsync(createChangeSetAsyncRequest).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Upload the change set contents asynchronously.
        /// </summary>
        /// <param name="uploadURL">The URL to upload the contents to.</param>
        /// <returns>Does not return anything (no exception means success).</returns>
        private async Task UploadChangeSetContents(string uploadURL)
        {
            // Generate a change set to create many nodes
            var changeSetContent = new ChangeNodeRequest[ChangeSetNodeCount];

            for (int nodeIdx = 0; nodeIdx < ChangeSetNodeCount; ++nodeIdx)
            {
                changeSetContent[nodeIdx] = new ChangeNodeRequest
                {
                    Id = string.Format(ChangeSetNodeIDPattern, nodeIdx), // Optional (if not specified, the service will auto-generate it)
                    Name = $"Change set node {nodeIdx}", // Optional
                    Links = new string[ChangeSetNodeLinkCount], // Optional
                };

                for (int linkIdx = 0; linkIdx < ChangeSetNodeLinkCount; ++linkIdx)
                {
                    changeSetContent[nodeIdx].Links[linkIdx] = $"frn:DemoLink-{nodeIdx}-{linkIdx}";
                }
            }

            Console.WriteLine("Uploading the change set contents...");

            await this.changeSetClient.PutAsNDJsonAsync(uploadURL, changeSetContent).ConfigureAwait(false);
        }

        /// <summary>
        /// Batch-get multiple trees and multiple nodes.
        /// </summary>
        /// <param name="trees">The identifying data for the trees to get.</param>
        /// <param name="nodes">The identifying data for the nodes to get.</param>
        /// <returns>Does not return anything.</returns>
        private async Task BatchGet(TreeIdentity[] trees, TreeNodeIdentity[] nodes)
        {
            var batchGetRequest = new BatchGetRequest
            {
                Trees = trees, // Optional
                Nodes = nodes, // Optional
            };

            Console.WriteLine($"Batch-getting {batchGetRequest.Trees?.Length} trees and {batchGetRequest.Nodes?.Length} nodes...");

            var batchGetResponse = await this.orgClient.BatchGetAsync(batchGetRequest).ConfigureAwait(false);

            if (batchGetResponse.Responses != null)
            {
                Console.WriteLine($"Batch-got {batchGetResponse.Responses.Trees.Count} trees:");
                this.PrintTrees(batchGetResponse.Responses.Trees);

                Console.WriteLine($"Batch-got {batchGetResponse.Responses.Nodes.Count} nodes:");
                this.PrintNodes(batchGetResponse.Responses.Nodes);

                Console.WriteLine();
            }

            if (batchGetResponse.Errors != null)
            {
                if (batchGetResponse.Errors.Trees != null)
                {
                    Console.WriteLine("Tree errors:");
                    foreach (var treeError in batchGetResponse.Errors.Trees)
                    {
                        Console.WriteLine($"{treeError.Code} {treeError.Message}");
                        if (treeError.Item != null)
                        {
                            Console.WriteLine($" ForestId={treeError.Item.ForestId}, TreeId={treeError.Item.TreeId}");
                        }
                    }

                    Console.WriteLine();
                }

                if (batchGetResponse.Errors.Nodes != null)
                {
                    Console.WriteLine("Node errors:");
                    foreach (var nodeError in batchGetResponse.Errors.Nodes)
                    {
                        Console.WriteLine($"{nodeError.Code} {nodeError.Message}");
                        if (nodeError.Item != null)
                        {
                            Console.WriteLine($" ForestId={nodeError.Item.ForestId}, TreeId={nodeError.Item.TreeId}, NodeId={nodeError.Item.NodeId}");
                        }
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}