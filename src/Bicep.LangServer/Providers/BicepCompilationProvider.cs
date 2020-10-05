// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.FileSystem;
using Bicep.Core.Parser;
using Bicep.Core.SemanticModel;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Az;
using Bicep.LanguageServer.CompilationManager;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LanguageServer.Providers
{
    /// <summary>
    /// Creates compilation contexts.
    /// </summary>
    /// <remarks>This class exists only so we can mock fatal exceptions in tests.</remarks>
    public class BicepCompilationProvider: ICompilationProvider
    {
        private readonly IResourceTypeProvider resourceTypeProvider;
        private readonly IFileResolver fileResolver;

        public BicepCompilationProvider(IResourceTypeProvider resourceTypeProvider, IFileResolver fileResolver)
        {
            this.resourceTypeProvider = resourceTypeProvider;
            this.fileResolver = fileResolver;
        }

        public CompilationContext Create(DocumentUri documentUri, string text)
        {
            var compilationCollection = CompilationCollection.CreateWithPreloadedMainFile(fileResolver, resourceTypeProvider, documentUri.GetFileSystemPath(), text);

            return new CompilationContext(compilationCollection, documentUri);
        }
    }
}