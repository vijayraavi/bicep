// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.SemanticModel;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.UnitTests.Utils
{
    public static class CompilationHelper
    {
        private const string SingleFileName = "main.bicep";

        public static CompilationCollection CreateCollectionWithSingleFile(IResourceTypeProvider resourceTypeProvider, string text)
            => CompilationCollection.Create(
                CreateSingleFileResolver(SingleFileName, text),
                resourceTypeProvider,
                SingleFileName);

        public static Compilation CreateForText(string text)
            => GetSingleFileCompilation(CreateCollectionWithSingleFile(TestResourceTypeProvider.Create(), text));

        public static Compilation CreateForText(IResourceTypeProvider resourceTypeProvider, string text)
            => GetSingleFileCompilation(CreateCollectionWithSingleFile(resourceTypeProvider, text));

        public static Compilation GetSingleFileCompilation(CompilationCollection compilationCollection)
            => compilationCollection.TryGetCompilation(SingleFileName, out _) ?? throw new InvalidOperationException();

        public static IFileResolver CreateSingleFileResolver(string fileName, string fileContents)
            => new InMemoryFileResolver(new Dictionary<string, string> { [fileName] = fileContents });
    }
}