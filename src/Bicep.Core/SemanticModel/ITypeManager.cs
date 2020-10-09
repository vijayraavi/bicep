// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using Bicep.Core.Diagnostics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.SemanticModel
{
    public interface ITypeManager
    {
        TypeSymbol GetTypeInfo(SyntaxBase syntax);

        TypeAssignment GetTypeAssignment(SyntaxBase syntax);

        IEnumerable<Diagnostic> GetAllDiagnostics();
    }
}