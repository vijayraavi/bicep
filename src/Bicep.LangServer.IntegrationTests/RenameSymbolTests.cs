﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Bicep.Core.Navigation;
using Bicep.Core.Parser;
using Bicep.Core.Samples;
using Bicep.Core.SemanticModel;
using Bicep.Core.Syntax;
using Bicep.Core.Syntax.Visitors;
using Bicep.Core.Text;
using Bicep.Core.UnitTests.Utils;
using Bicep.LangServer.IntegrationTests.Extensions;
using Bicep.LanguageServer.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SymbolKind = Bicep.Core.SemanticModel.SymbolKind;

namespace Bicep.LangServer.IntegrationTests
{
    [TestClass]
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test methods do not need to follow this convention.")]
    public class RenameSymbolTests
    {
        [DataTestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method, DynamicDataDisplayNameDeclaringType = typeof(DataSet), DynamicDataDisplayName = nameof(DataSet.GetDisplayName))]
        public async Task RenamingIdentifierAccessOrDeclarationShouldRenameDeclarationAndAllReferences(DataSet dataSet)
        {
            var uri = DocumentUri.From($"/{dataSet.Name}");

            using var client = await IntegrationTestHelper.StartServerWithTextAsync(dataSet.Bicep, uri);
            var compilation = new Compilation(TestResourceTypeProvider.Create(), SyntaxFactory.CreateFromText(dataSet.Bicep));
            var symbolTable = compilation.ReconstructSymbolTable();
            var lineStarts = TextCoordinateConverter.GetLineStarts(dataSet.Bicep);

            var symbolToSyntaxLookup = symbolTable
                .Where(pair => pair.Value.Kind != SymbolKind.Error)
                .ToLookup(pair => pair.Value, pair => pair.Key);

            var validVariableAccessPairs = symbolTable
                .Where(pair => (pair.Key is VariableAccessSyntax || pair.Key is IDeclarationSyntax)
                               && pair.Value.Kind != SymbolKind.Error
                               && pair.Value.Kind != SymbolKind.Function
                               // symbols whose identifiers have parse errors will have a name like <error> or <missing>
                               && pair.Value.Name.Contains("<") == false);

            const string expectedNewText = "NewIdentifier";
            foreach (var (syntax, symbol) in validVariableAccessPairs)
            {
                var edit = await client.RequestRename(new RenameParams
                {
                    NewName = expectedNewText,
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = PositionHelper.GetPosition(lineStarts, syntax.Span.Position)
                });

                edit.DocumentChanges.Should().BeNullOrEmpty();
                edit.Changes.Should().HaveCount(1);
                edit.Changes.Should().ContainKey(uri);

                var textEdits = edit.Changes[uri];
                textEdits.Should().NotBeEmpty();

                var expectedEdits = symbolToSyntaxLookup[symbol]
                    .Select(node => CreateExpectedTextEdit(lineStarts, expectedNewText, node));

                textEdits.Should().BeEquivalentTo(expectedEdits);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method, DynamicDataDisplayNameDeclaringType = typeof(DataSet), DynamicDataDisplayName = nameof(DataSet.GetDisplayName))]
        public async Task RenamingFunctionsShouldProduceEmptyEdit(DataSet dataSet)
        {
            var uri = DocumentUri.From($"/{dataSet.Name}");

            using var client = await IntegrationTestHelper.StartServerWithTextAsync(dataSet.Bicep, uri);
            var compilation = new Compilation(TestResourceTypeProvider.Create(), SyntaxFactory.CreateFromText(dataSet.Bicep));
            var symbolTable = compilation.ReconstructSymbolTable();
            var lineStarts = TextCoordinateConverter.GetLineStarts(dataSet.Bicep);

            var validFunctionCallPairs = symbolTable
                .Where(pair => pair.Value.Kind == SymbolKind.Function)
                .Select(pair=>pair.Key);

            foreach (var syntax in validFunctionCallPairs)
            {
                var edit = await client.RequestRename(new RenameParams
                {
                    NewName = "NewIdentifier",
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = PositionHelper.GetPosition(lineStarts, syntax.Span.Position)
                });

                edit.DocumentChanges.Should().BeNullOrEmpty();
                edit.Changes.Should().BeNull();
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method, DynamicDataDisplayNameDeclaringType = typeof(DataSet), DynamicDataDisplayName = nameof(DataSet.GetDisplayName))]
        public async Task RenamingNonSymbolsShouldProduceEmptyEdit(DataSet dataSet)
        {
            // local function
            bool IsWrongNode(SyntaxBase node) => !(node is ISymbolReference) && !(node is IDeclarationSyntax) && !(node is Token);

            var uri = DocumentUri.From($"/{dataSet.Name}");

            using var client = await IntegrationTestHelper.StartServerWithTextAsync(dataSet.Bicep, uri);
            var compilation = new Compilation(TestResourceTypeProvider.Create(), SyntaxFactory.CreateFromText(dataSet.Bicep));
            var symbolTable = compilation.ReconstructSymbolTable();
            var lineStarts = TextCoordinateConverter.GetLineStarts(dataSet.Bicep);

            var symbolToSyntaxLookup = symbolTable
                .Where(pair => pair.Value.Kind != SymbolKind.Error)
                .ToLookup(pair => pair.Value, pair => pair.Key);

            var wrongNodes = SyntaxAggregator.Aggregate(
                compilation.ProgramSyntax,
                new List<SyntaxBase>(),
                (accumulated, node) =>
                {
                    if (IsWrongNode(node) && !(node is ProgramSyntax))
                    {
                        accumulated.Add(node);
                    }

                    return accumulated;
                },
                accumulated => accumulated,
                (accumulated, node) => IsWrongNode(node));

            foreach (var syntax in wrongNodes)
            {
                var edit = await client.RequestRename(new RenameParams
                {
                    NewName = "NewIdentifier",
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = PositionHelper.GetPosition(lineStarts, syntax.Span.Position)
                });

                edit.DocumentChanges.Should().BeNullOrEmpty();
                edit.Changes.Should().BeNull();
            }
        }

        private static TextEdit CreateExpectedTextEdit(ImmutableArray<int> lineStarts, string newText, SyntaxBase syntax) =>
            new TextEdit
            {
                NewText = newText,
                Range = PositionHelper.GetNameRange(lineStarts, syntax)
            };

        private static IEnumerable<object[]> GetData()
        {
            return DataSets.AllDataSets.ToDynamicTestData();
        }
    }
}
