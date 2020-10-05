// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Immutable;
using Bicep.Core.Parser;
using Bicep.Core.SemanticModel;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LanguageServer.CompilationManager
{
    public class CompilationContext
    {
        public CompilationContext(CompilationCollection compilationCollection, DocumentUri mainDocumentUri)
        {
            var maybeCompilation = compilationCollection.TryGetCompilation(mainDocumentUri.GetFileSystemPath(), out var resolutionFailure);

            CompilationCollection = compilationCollection;
            Compilation = maybeCompilation ?? throw new InvalidOperationException($"Resolving file {mainDocumentUri} failed: '{resolutionFailure}'.");
        }

        public Compilation Compilation { get; }

        public CompilationCollection CompilationCollection { get; }

        public ImmutableArray<int> LineStarts => Compilation.LineStarts;
    }
}
