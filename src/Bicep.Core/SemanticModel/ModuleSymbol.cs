// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using Bicep.Core.Syntax;
using Bicep.Core.Diagnostics;
using System;

namespace Bicep.Core.SemanticModel
{
    public class ModuleSymbol : DeclaredSymbol
    {
        public ModuleSymbol(ISymbolContext context, string name, ModuleDeclarationSyntax declaringSyntax, SyntaxBase body)
            : base(context, name, declaringSyntax, declaringSyntax.Name)
        {
            this.Body = body;
        }

        public ModuleDeclarationSyntax DeclaringModule => (ModuleDeclarationSyntax) this.DeclaringSyntax;

        public SyntaxBase Body { get; }

        public override void Accept(SymbolVisitor visitor) => visitor.VisitModuleSymbol(this);

        public override SymbolKind Kind => SymbolKind.Module;

        public Compilation? TryGetReferencedCompilation(out ErrorDiagnostic? failureDiagnostic)
        {
            var pathSyntax = DeclaringModule.TryGetPath();
            if (pathSyntax == null)
            {
                failureDiagnostic = DiagnosticBuilder.ForPosition(DeclaringModule.Path).UnableToFindPathForModule();
                return null;
            }

            var moduleFileName = pathSyntax.TryGetLiteralValue();
            if (moduleFileName == null)
            {
                failureDiagnostic = DiagnosticBuilder.ForPosition(DeclaringModule.Path).ModulePathInterpolationUnsupported();
                return null;
            }

            var compilation = Context.CompilationCollection.TryGetCompilationForModule(Context.Compilation, moduleFileName, out var failureMessage);

            failureDiagnostic = failureMessage != null ? DiagnosticBuilder.ForPosition(DeclaringModule.Path).UnableToFindPathForModuleWithError(failureMessage) : null;
            return compilation;
        }

        public override IEnumerable<Symbol> Descendants
        {
            get
            {
                yield return this.Type;
            }
        }
    }
}