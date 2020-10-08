// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem;
using Bicep.Core.Utils;

namespace Bicep.Core.SemanticModel
{
    public class CompilationCollection
    {
        private class CachedCompilation
        {
            public CachedCompilation(Compilation? compilation, string? loadFailureMessage)
            {
                Compilation = compilation;
                LoadFailureMessage = loadFailureMessage;
            }

            public Compilation? Compilation { get; }

            public string? LoadFailureMessage { get; }
        }

        private readonly IFileResolver fileResolver;
        private readonly IResourceTypeProvider resourceTypeProvider;
        private IDictionary<string, CachedCompilation> compilations;

        private CompilationCollection(IFileResolver fileResolver, IResourceTypeProvider resourceTypeProvider)
        {
            this.fileResolver = fileResolver;
            this.resourceTypeProvider = resourceTypeProvider;
            this.compilations = new Dictionary<string, CachedCompilation>();
        }

        public static CompilationCollection Create(IFileResolver fileResolver, IResourceTypeProvider resourceTypeProvider, string initialFileName)
        {
            var compilationCollection = new CompilationCollection(fileResolver, resourceTypeProvider);

            var moduleGraph = new Dictionary<Compilation, HashSet<Compilation>>();
            compilationCollection.PopulateRecursive(initialFileName, moduleGraph);

            var moduleLookup = moduleGraph
                .SelectMany(kvp => kvp.Value.Select(x => (kvp.Key, x)))
                .ToLookup(x => x.Key, x => x.Item1);

            CycleDetector<Compilation>.FindCycles(moduleLookup);

            return compilationCollection;
        }

        public static CompilationCollection CreateWithPreloadedMainFile(IFileResolver fileResolver, IResourceTypeProvider resourceTypeProvider, string initialFileName, string initialFileContents)
        {
            var compilationCollection = new CompilationCollection(fileResolver, resourceTypeProvider);

            compilationCollection.RegisterCompilation(initialFileName, initialFileContents);

            var moduleGraph = new Dictionary<Compilation, HashSet<Compilation>>();
            compilationCollection.PopulateRecursive(initialFileName, moduleGraph);

            var moduleLookup = moduleGraph
                .SelectMany(kvp => kvp.Value.Select(x => (kvp.Key, x)))
                .ToLookup(x => x.Key, x => x.Item1);

            CycleDetector<Compilation>.FindCycles(moduleGraph);

            return compilationCollection;
        }

        public Compilation? TryGetCompilationForModule(Compilation parentCompilation, string fileName, out string? failureMessage)
        {
            var moduleFileName = fileResolver.TryResolveModulePath(fileName, parentCompilation.FileName);
            if (moduleFileName == null)
            {
                failureMessage = $"Unable to resolve path for module \"{fileName}\"";
                return null;
            }

            return TryGetCompilation(moduleFileName, out failureMessage);
        }

        private void RegisterCompilation(string fullFileName, string fileContents)
        {
            var normalizedFileName = fileResolver.GetNormalizedFileName(fullFileName);

            var programSyntax = SyntaxFactory.CreateFromText(fileContents);
            var lineStarts = TextCoordinateConverter.GetLineStarts(fileContents);

            var compilation = new Compilation(this, resourceTypeProvider, programSyntax, lineStarts, normalizedFileName);

            compilations[normalizedFileName] = new CachedCompilation(compilation, null);
        }

        public Compilation? TryGetCompilation(string fullFileName, out string? failureMessage)
        {
            var normalizedFileName = fileResolver.GetNormalizedFileName(fullFileName);
 
            if (compilations.TryGetValue(normalizedFileName, out var cachedCompilation))
            {
                failureMessage = cachedCompilation.LoadFailureMessage;
                return cachedCompilation.Compilation;
            }

            var fileContents = fileResolver.TryRead(normalizedFileName, out failureMessage);
            if (fileContents == null)
            {
                compilations[normalizedFileName] = new CachedCompilation(null, failureMessage);
                return null;
            }

            var programSyntax = SyntaxFactory.CreateFromText(fileContents);
            var lineStarts = TextCoordinateConverter.GetLineStarts(fileContents);

            var compilation = new Compilation(this, resourceTypeProvider, programSyntax, lineStarts, normalizedFileName);
            compilations[normalizedFileName] = new CachedCompilation(compilation, null);

            return compilation;
        }

        private void PopulateRecursive(string fileName, Dictionary<Compilation, HashSet<Compilation>> moduleGraph)
        {
            var compilation = TryGetCompilation(fileName, out _);
            if (compilation == null)
            {
                return;
            }

            var modules = compilation.ProgramSyntax.Declarations.OfType<ModuleDeclarationSyntax>();

            foreach (var module in modules)
            {
                var moduleFileName = TryGetNormalizedModulePath(fileName, module);
                if (moduleFileName == null)
                {
                    // File load failed, but we can ignore for now; we'll raise diagnostics for this during compilation.
                    continue;
                }

                if (!compilations.ContainsKey(moduleFileName))
                {
                    PopulateRecursive(moduleFileName, moduleGraph);
                }

                // For the purposes of cycle detection, we only need to care about files that have been loaded.
                // Files that haven't been loaded can't declare any modules!
                if (!moduleGraph.TryGetValue(compilation, out var childrenHashSet))
                {
                    childrenHashSet = new HashSet<Compilation>();
                    moduleGraph[compilation] = childrenHashSet;
                }

                var moduleCompilation = compilations[moduleFileName].Compilation;
                if (moduleCompilation != null)
                {
                    childrenHashSet.Add(moduleCompilation);
                }
            }
        }

        private string? TryGetNormalizedModulePath(string parentFileName, ModuleDeclarationSyntax moduleDeclarationSyntax)
        {
            var pathName = SyntaxHelper.TryGetModulePath(moduleDeclarationSyntax);
            if (pathName == null)
            {
                return null;
            }

            var fullPath = fileResolver.TryResolveModulePath(pathName, parentFileName);
            if (fullPath == null)
            {
                return null;
            }

            return fullPath;
        }

        public bool EmitDiagnosticsAndCheckSuccess(Action<Compilation, Diagnostic> onDiagnostic)
        {
            var success = true;
            foreach (var cachedCompilation in compilations.Values)
            {
                var compilation = cachedCompilation.Compilation;
                if (compilation == null)
                {
                    continue;
                }

                foreach (var diagnostic in compilation.GetSemanticModel().GetAllDiagnostics())
                {
                    success &= diagnostic.Level != DiagnosticLevel.Error;
                    onDiagnostic(compilation, diagnostic);
                }
            }

            return success;
        }        
    }
}