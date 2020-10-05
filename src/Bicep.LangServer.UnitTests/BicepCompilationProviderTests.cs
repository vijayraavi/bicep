// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.Samples;
using Bicep.Core.UnitTests.Utils;
using Bicep.LanguageServer.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LangServer.UnitTests
{
    [TestClass]
    public class BicepCompilationProviderTests
    {
        [TestMethod]
        public void Create_ShouldReturnValidCompilation()
        {
            var documentUri = DocumentUri.File("/my/mock/path/main.bicep");
            var provider = new BicepCompilationProvider(TestResourceTypeProvider.Create(), CompilationHelper.CreateSingleFileResolver(documentUri.GetFileSystemPath(), DataSets.Parameters_LF.Bicep));

            var context = provider.Create(documentUri, DataSets.Parameters_LF.Bicep);

            context.Compilation.Should().NotBeNull();
            context.Compilation.GetSemanticModel().GetAllDiagnostics().Should().BeEmpty();
            context.LineStarts.Should().NotBeEmpty();
            context.LineStarts[0].Should().Be(0);
        }
    }
}

