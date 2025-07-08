//-----------------------------------------------------------------------
// <copyright file="OrgDemo.AdditionalProperties.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Trimble.Connect.Org.Client;

    /// <summary>
    /// The Organizer service .NET SDK demo.
    /// This part of the class demonstrates how to use the <see cref="OrgModel.AdditionalProperties"/> property
    /// to launch requests and handle responses that are not yet "officially" supported in the .NET SDK.
    /// The <see cref="OrgModel.AdditionalProperties"/> property allows using request and response properties which
    /// are recognized by the service but not yet implemented in the .NET SDK.
    /// This makes it possible to immediately use the new functionality as it is released in the service,
    /// without the need to wait for it to also be implemented in the .NET SDK.
    /// </summary>
    public partial class OrgDemo
    {
        /// <summary>
        /// Demonstrate using the AdditionalProperties property in requests and responses.
        /// </summary>
        /// <param name="forestID">The ID of the forest in which to work with trees and nodes.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunAdditionalPropertiesDemo(string forestID)
        {
            Tree createdTree = await this.CreateTreeUsingAdditionalProperties(forestID).ConfigureAwait(false);

            Tree gotTree = await this.GetTreeUsingAdditionalProperties(createdTree.ForestId, createdTree.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a tree by using <see cref="OrgModel.AdditionalProperties"/> and <see cref="OrgRequest.AdditionalQueryParameters"/> in the request
        /// to mimic an existing request with a not yet supported one (in the .NET SDK)
        /// and interpret the response based on the <see cref="OrgModel.AdditionalProperties"/> .
        /// </summary>
        /// <param name="forestID">The forest ID of the tree to get.</param>
        /// <param name="treeID">The ID of the tree to get.</param>
        /// <returns>The tree.</returns>
        private async Task<Tree> GetTreeUsingAdditionalProperties(string forestID, string treeID)
        {
            var getTreeRequest = new DemoGetTreeRequest
            {
                // This is how the OrgModel.AdditionalProperties can be used to specify request properties
                // not yet supported by the .NET SDK (but already supported by the service).
                AdditionalProperties = new Dictionary<string, JToken>
                {
                    { "ForestId", forestID },
                    { "TreeId", treeID },
                },

                // This is how the OrgRequest.AdditionalQueryParameters to specify query parameters
                // not yet supported by the .NET SDK (but already supported by the service).
                AdditionalQueryParameters = new Dictionary<string, string> // Optional
                {
                    { "deleted", "true" },
                },
            };

            Console.WriteLine($"Getting tree (using additional properties) with ForestId={getTreeRequest.AdditionalProperties["ForestId"]}, TreeId={getTreeRequest.AdditionalProperties["TreeId"]}...");

            DemoEmptyOrgModel responseTree = await this.orgClient.SendAsync<DemoEmptyOrgModel>(getTreeRequest).ConfigureAwait(false);

            Tree tree = new Tree
            {
                ForestId = responseTree.AdditionalProperties["forestId"].ToString(),
                Id = responseTree.AdditionalProperties["id"].ToString(),
                Name = responseTree.AdditionalProperties["name"].ToString(),
                Description = responseTree.AdditionalProperties["description"].ToString(),
                Type = responseTree.AdditionalProperties["type"].ToString(),
                CreatedAt = (DateTime)responseTree.AdditionalProperties["createdAt"],
                ModifiedAt = (DateTime)responseTree.AdditionalProperties["modifiedAt"],
                CreatedBy = responseTree.AdditionalProperties["createdBy"].ToString(),
                ModifiedBy = responseTree.AdditionalProperties["modifiedBy"].ToString(),
                Deleted = responseTree.AdditionalProperties.ContainsKey("deleted"),
                Version = (int)responseTree.AdditionalProperties["v"],
            };

            Console.Write("Got tree: ");
            this.PrintTree(tree);
            Console.WriteLine();

            return tree;
        }

        /// <summary>
        /// Create a new tree by using additional properties in the request.
        /// </summary>
        /// <param name="forestID">The ID of the forest in which to create the tree.</param>
        /// <returns>The created tree.</returns>
        private async Task<Tree> CreateTreeUsingAdditionalProperties(string forestID)
        {
            var createTreeRequest = new CreateTreeRequest
            {
                ForestId = forestID,
                AdditionalProperties = new Dictionary<string, JToken>
                {
                    { "Name", "Demo tree " },
                    { "Description", "Demo tree" },
                    { "Type", "DemoTree" },
                },
            };

            Console.WriteLine($"Creating tree (using additional properties) with ForestId={createTreeRequest.ForestId}, Name={createTreeRequest.AdditionalProperties["Name"]}, " +
                $"Description={createTreeRequest.AdditionalProperties["Description"]}, Type={createTreeRequest.AdditionalProperties["Type"]}...");

            Tree createdTree = await this.orgClient.CreateTreeAsync(createTreeRequest).ConfigureAwait(false);

            Console.Write("Created tree: ");
            this.PrintTree(createdTree);
            Console.WriteLine();

            return createdTree;
        }

        /// <summary>
        /// A generic derived class of OrgModel, with no properties (basically empty),
        /// used for demonstrating the usage of the <see cref="OrgModel.AdditionalProperties"/> property in responses.
        /// It is used to mimic an existing concrete derived class of <see cref="OrgModel"/>, such as <see cref="Tree"/>.
        /// In the same way it is possible to emulate a response class that is already supported by the service,
        /// but not yet supported by the .NET SDK.
        /// </summary>
        private class DemoEmptyOrgModel : OrgModel
        {
        }

        /// <summary>
        /// An artificial version of the <see cref="GetTreeRequest"/> used for demonstrating the usage of <see cref="OrgModel.AdditionalProperties"/> in requests.
        /// Just as the <see cref="OrgModel.AdditionalProperties"/> property is used in this request class to refer to currently supported request properties (in the .NET SDK),
        /// it can also be used to refer to request properties which may be already supported by the service but not yet supported by the .NET SDK.
        /// </summary>
        private class DemoGetTreeRequest : OrgRequest
        {
            /// <summary>
            /// Demonstrates how to override the request URL.
            /// </summary>
            [JsonIgnore]
            public override string URI => $"forests/{this.AdditionalProperties["ForestId"]}/trees/{this.AdditionalProperties["TreeId"]}";

            /// <summary>
            /// Demonstrates how to override the request method.
            /// </summary>
            [JsonIgnore]
            public override HttpMethod Method => HttpMethod.Get;

            /// <summary>
            /// Demonstrates how to override the request validation.
            /// </summary>
            public override void Validate()
            {
                base.Validate();

                this.ThrowIfPropertyIsNull(this.AdditionalProperties["ForestId"], "ForestId");
                this.ThrowIfPropertyIsNull(this.AdditionalProperties["TreeId"], "TreeId");
            }
        }
    }
}