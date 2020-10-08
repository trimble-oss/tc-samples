//-----------------------------------------------------------------------
// <copyright file="PSetDemo.BulkOperations.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;

    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The Property Set service .NET SDK demo.
    /// This part of the class demonstrates bulk operations that handle large amounts of data (change sets and batch gets).
    /// </summary>
    public partial class PSetDemo
    {
        /// <summary>
        /// How many PSets to have in the change set
        /// </summary>
        private const int ChangeSetPSetCount = 25;

        /// <summary>
        /// The link pattern to be used in change set PSets
        /// </summary>
        private const string ChangeSetLinkPattern = "frn:DemoLink-{0}"; // Where {0} is the index of the PSet

        /// <summary>
        /// The change set client used to handle large amounts of data.
        /// </summary>
        private HttpClient changeSetClient = null;

        /// <summary>
        /// Demonstrate change set and batch get functionality.
        /// </summary>
        /// <param name="parentDefinition">The parent definition for which PSets will be created.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunBulkOperationsDemo(Definition parentDefinition)
        {
            // Asynchronous change set demo
            await this.RunAsyncChangeSetWorkflow(parentDefinition).ConfigureAwait(false);

            // Batch-get multiple libraries, definitions and PSets
            LibraryIdentity[] libs = new LibraryIdentity[]
            {
                new LibraryIdentity
                {
                    LibraryId = parentDefinition.LibraryId,
                },
                new LibraryIdentity
                {
                    LibraryId = Guid.NewGuid().ToString(), // Inexistent, will not be found
                }
            };

            DefinitionIdentity[] defs = new DefinitionIdentity[]
            {
                new DefinitionIdentity
                {
                    LibraryId = parentDefinition.LibraryId,
                    DefinitionId = parentDefinition.Id,
                },
                new DefinitionIdentity
                {
                    LibraryId = parentDefinition.LibraryId,
                    DefinitionId = Guid.NewGuid().ToString(), // Inexistent, will not be found
                },
            };

            PSetIdentity[] psets = new PSetIdentity[ChangeSetPSetCount];
            for (int psetIdx = 0; psetIdx < ChangeSetPSetCount; ++psetIdx)
            {
                psets[psetIdx] = new PSetIdentity
                {
                    LibraryId = parentDefinition.LibraryId,
                    DefinitionId = parentDefinition.Id,
                    Link = string.Format(ChangeSetLinkPattern, psetIdx),
                };
            }

            await this.BatchGet(libs, defs, psets).ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrate the full asynchronous change set workflow (create change set, upload contents, check results).
        /// </summary>
        /// <param name="parentDefinition">The definition for which to create PSets using the change set.</param>
        /// <returns>Does not return anything.</returns>
        private async Task RunAsyncChangeSetWorkflow(Definition parentDefinition)
        {
            if (parentDefinition != null)
            {
                // Create an asynchronous change set (declare intention to upload changes)
                ChangeSetResponseInitiated createAsyncChangesSetResponse = await this.CreateAsyncChangeset().ConfigureAwait(false);

                if (string.Compare(createAsyncChangesSetResponse.Status, "WaitingUpload", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Upload the change set contents asynchronously
                    await this.UploadChangeSetContents(createAsyncChangesSetResponse.UploadURL, parentDefinition).ConfigureAwait(false);

                    // Wait until the change set has finished processing.
                    // Use a change set status checking algorithm with expoenential backoff an jitter
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
                        Console.WriteLine($"Waiting {(int)checkChangeSetStatusInterval / 1000.0} sec...");
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
                            await this.changeSetClient.GetNDJsonAsync<PSet>(getChangeSetStatusResponse.ResultsURL, this.PrintPSets).ConfigureAwait(false);
                        }

                        // Get the change set errors
                        if (!string.IsNullOrEmpty(getChangeSetStatusResponse.ErrorsURL))
                        {
                            Console.WriteLine("Change set errors: ");
                            await this.changeSetClient.GetNDJsonAsync<PSet>(getChangeSetStatusResponse.ErrorsURL, this.PrintPSets).ConfigureAwait(false);
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
                ChangeSetID = changeSetID,
            };

            Console.Write($"Getting status of change set {changeSetID}... ");

            ChangeSetResponse changeSetStatusResponse = await this.psetClient.GetChangeSetStatusAsync(getChangeSetStatusRequest).ConfigureAwait(false);

            Console.WriteLine(changeSetStatusResponse.Status);

            return changeSetStatusResponse;
        }

        /// <summary>
        /// Create an asynchronous change set.
        /// </summary>
        /// <returns>The change set creation response.</returns>
        private async Task<ChangeSetResponseInitiated> CreateAsyncChangeset()
        {
            var createChangeSetAsyncRequest = new CreateChangeSetAsyncRequest
            {
            };

            Console.WriteLine("Creating asynchronous change set...");

            return await this.psetClient.CreateChangeSetAsync(createChangeSetAsyncRequest).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload the change set contents asynchronously.
        /// </summary>
        /// <param name="uploadURL">The URL to upload the contents to.</param>
        /// <param name="parentDefinition">The definition for which to create PSets using the change set.</param>
        /// <returns>Does not return anything (no exception means success).</returns>
        private async Task UploadChangeSetContents(string uploadURL, Definition parentDefinition)
        {
            if (parentDefinition != null)
            {
                // Generate a change set to create many PSets
                var changeSetContent = new ChangePSetRequest[ChangeSetPSetCount];

                for (int psetIdx = 0; psetIdx < ChangeSetPSetCount; ++psetIdx)
                {
                    var props = new JObject();
                    props["str"] = $"DemoStringValue-{psetIdx}";
                    props["num"] = psetIdx;

                    changeSetContent[psetIdx] = new ChangePSetRequest
                    {
                        LibraryId = parentDefinition.LibraryId,
                        DefinitionId = parentDefinition.Id,
                        Link = string.Format(ChangeSetLinkPattern, psetIdx),
                        Props = props,
                    };
                }

                Console.WriteLine("Uploading the change set contents...");

                await this.changeSetClient.PutAsNDJsonAsync(uploadURL, changeSetContent).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Batch-get multiple libraries, definitions and PSets.
        /// </summary>
        /// <param name="libraries">The identifying data for the libraries to get.</param>
        /// <param name="definitions">The identifying data for the definitions to get.</param>
        /// <param name="psets">The identifying data for the PSets to get.</param>
        /// <returns>Does not return anything.</returns>
        private async Task BatchGet(LibraryIdentity[] libraries, DefinitionIdentity[] definitions, PSetIdentity[] psets)
        {
            var batchGetRequest = new BatchGetRequest
            {
                Libraries = libraries, // Optional
                Definitions = definitions, // Optional
                PSets = psets, // Optional
            };

            Console.WriteLine($"Batch-getting {batchGetRequest.Libraries?.Count} libraries, {batchGetRequest.Definitions.Count} definitions and {batchGetRequest.PSets?.Count} PSets...");

            var batchGetResponse = await this.psetClient.BatchGetAsync(batchGetRequest).ConfigureAwait(false);

            if (batchGetResponse.Responses != null)
            {
                Console.WriteLine($"Batch-got {batchGetResponse.Responses.Libraries.Count} libraries:");
                this.PrintLibraries(batchGetResponse.Responses.Libraries);

                Console.WriteLine($"Batch-got {batchGetResponse.Responses.Definitions.Count} definitions:");
                this.PrintDefinitions(batchGetResponse.Responses.Definitions);

                Console.WriteLine($"Batch-got {batchGetResponse.Responses.PSets.Count} PSets:");
                this.PrintPSets(batchGetResponse.Responses.PSets);

                Console.WriteLine();
            }

            if (batchGetResponse.Errors != null)
            {
                if (batchGetResponse.Errors.Libraries != null)
                {
                    Console.WriteLine("Library errors:");
                    foreach (var libraryError in batchGetResponse.Errors.Libraries)
                    {
                        Console.WriteLine($"{libraryError.Code} {libraryError.Message}");
                        if (libraryError.Item != null)
                        {
                            Console.WriteLine($" LibraryId={libraryError.Item.LibraryId}");
                        }
                    }

                    Console.WriteLine();
                }

                if (batchGetResponse.Errors.Definitions != null)
                {
                    Console.WriteLine("Definition errors:");
                    foreach (var definitionError in batchGetResponse.Errors.Definitions)
                    {
                        Console.WriteLine($"{definitionError.Code} {definitionError.Message}");
                        if (definitionError.Item != null)
                        {
                            Console.WriteLine($" LibraryId={definitionError.Item.LibraryId}, DefinitionId={definitionError.Item.DefinitionId}");
                        }
                    }

                    Console.WriteLine();
                }

                if (batchGetResponse.Errors.PSets != null)
                {
                    Console.WriteLine("PSet errors:");
                    foreach (var psetError in batchGetResponse.Errors.PSets)
                    {
                        Console.WriteLine($"{psetError.Code} {psetError.Message}");
                        if (psetError.Item != null)
                        {
                            Console.WriteLine($" LibraryId={psetError.Item.LibraryId}, DefinitionId={psetError.Item.DefinitionId}, Link={psetError.Item.Link}");
                        }
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}