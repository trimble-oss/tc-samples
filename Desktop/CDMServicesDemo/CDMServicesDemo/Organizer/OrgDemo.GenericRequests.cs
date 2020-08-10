//-----------------------------------------------------------------------
// <copyright file="OrgDemo.GenericRequests.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;

    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class demonstrates how to use generic requests to access functionality of the service
    /// which is not yet directly implemented in the .NET SDK.
    /// This makes it possible to immediately use the new functionality as it is released in the service,
    /// without the need to wait for it to also be implemented in the .NET SDK.
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// Demonstrate using the generic requests.
        /// </summary>
        /// <param name="forestID">The ID of the forest in which to work with trees and nodes.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunGenericRequestsDemo(string forestID)
        {
            Tree createdTree = await this.CreateTreeUsingGenericRequest(forestID).ConfigureAwait(false);

            Tree gotTree = await this.GetTreeUsingGenericRequest(createdTree.ForestId, createdTree.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a tree using a generic request, with explicitly specified arguments.
        /// </summary>
        /// <param name="forestID">The forest ID of the tree to get.</param>
        /// <param name="treeID">The ID of the tree to get.</param>
        /// <returns>The tree.</returns>
        private async Task<Tree> GetTreeUsingGenericRequest(string forestID, string treeID)
        {
            Console.WriteLine($"Getting tree (using a generic request) with ForestId={forestID}, TreeId={treeID}...");

            Tree tree = await this.orgClient.SendAsync<JObject, Tree>(
                $"forests/{forestID}/trees/{treeID}",
                null,
                HttpMethod.Get,
                new Dictionary<string, string> { { "deleted", "true" } }).ConfigureAwait(false);

            Console.Write($"Got tree: ");
            this.PrintTree(tree);
            Console.WriteLine();

            return tree;
        }

        /// <summary>
        /// Tests creating a new tree using a generic request, with default arguments.
        /// </summary>
        /// <param name="forestID">The forest in which to create the tree.</param>
        /// <returns>The created tree.</returns>
        private async Task<Tree> CreateTreeUsingGenericRequest(string forestID)
        {
            var requestObj = new JObject();
            requestObj.Add("Name", "Demo tree " + Guid.NewGuid().ToString());
            requestObj.Add("Description", "Demo tree");
            requestObj.Add("Type", "DemoTree");

            Console.WriteLine($"Creating tree (using a generic request) with ForestId={forestID}, Name={requestObj["Name"].ToString()}, " +
                $"Description={requestObj["Description"].ToString()}, Type={requestObj["Type"].ToString()}...");

            Tree tree = await this.orgClient.SendAsync<JObject, Tree>($"forests/{forestID}/trees", requestObj).ConfigureAwait(false);

            Console.Write($"Created tree: ");
            this.PrintTree(tree);
            Console.WriteLine();

            return tree;
        }
    }
}