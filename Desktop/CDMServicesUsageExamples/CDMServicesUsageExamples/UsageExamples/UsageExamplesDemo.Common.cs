//-----------------------------------------------------------------------
// <copyright file="UsageExamplesDemo.Common.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesUsageExamples
{
    using System;
    using Trimble.Connect.PSet.Client;

    /// <summary>
    /// The usage examples demo.
    /// This part of the class contains common functionality.
    /// </summary>
    public partial class UsageExamplesDemo
    {
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

        /// <summary>
        /// Print the contents of a PSet.
        /// </summary>
        /// <param name="pset">The PSet to print.</param>
        private void PrintPSet(PSet pset)
        {
            if (pset != null)
            {
                Console.Write($"LibraryId={pset.LibraryId}, DefinitionId={pset.DefinitionId}, Link={pset.Link}," +
                    $"CreatedAt={pset.CreatedAt}, ModifiedAt={pset.ModifiedAt}, Deleted={pset.Deleted == true}, Version={pset.Version}, SchemaVersion={pset.SchemaVersion}");

                if (pset.Props != null)
                {
                    Console.Write(" Props:");
                    foreach (var prop in pset.Props)
                    {
                        Console.Write($" {prop.Key}={prop.Value}");
                    }
                }

                Console.WriteLine();
            }
        }
    }
}