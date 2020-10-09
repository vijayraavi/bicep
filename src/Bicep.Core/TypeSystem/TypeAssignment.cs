// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Linq;
using Bicep.Core.Diagnostics;

namespace Bicep.Core.TypeSystem
{
    public class TypeAssignment
    {
        public TypeAssignment(ITypeReference assignedType, ITypeReference declaredType)
            : this(assignedType, declaredType, Enumerable.Empty<Diagnostic>())
        {
        }

        public TypeAssignment(ITypeReference assignedType, ITypeReference declaredType, IEnumerable<Diagnostic> diagnostics)
        {
            this.AssignedType = assignedType;
            this.Diagnostics = diagnostics;
            this.DeclaredType = declaredType;
        }

        public ITypeReference AssignedType { get; }

        public ITypeReference DeclaredType { get; }

        public IEnumerable<Diagnostic> Diagnostics { get; }

        public TypeAssignment ReplaceDiagnostics(IEnumerable<Diagnostic> newDiagnostics) => new TypeAssignment(this.AssignedType, this.DeclaredType, newDiagnostics);
    }
}