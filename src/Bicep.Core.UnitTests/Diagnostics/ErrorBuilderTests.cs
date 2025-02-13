// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bicep.Core.Diagnostics;
using Bicep.Core.Parser;
using Bicep.Core.Resources;
using Bicep.Core.SemanticModel;
using Bicep.Core.TypeSystem;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Diagnostics
{
    [TestClass]
    public class DiagnosticBuilderTests
    {
        [TestMethod]
        public void DiagnosticBuilder_CodesAreUnique()
        {
            var diagnosticMethods = typeof(DiagnosticBuilder.DiagnosticBuilderInternal)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => typeof(Diagnostic).IsAssignableFrom(m.ReturnType));

            // verify the above Linq is actually working
            diagnosticMethods.Should().HaveCountGreaterThan(40);

            var builder = DiagnosticBuilder.ForPosition(new TextSpan(0, 10));

            var definedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var diagnosticMethod in diagnosticMethods)
            {
                var mockParams = diagnosticMethod.GetParameters().Select(CreateMockParameter);
                
                var diagnostic = diagnosticMethod.Invoke(builder, mockParams.ToArray()) as Diagnostic;

                // verify that the Code is unique
                definedCodes.Should().NotContain(diagnostic!.Code, $"Method {diagnosticMethod.Name} should be assigned a unique error code.");
                definedCodes.Add(diagnostic!.Code);
            }
        }

        private static object CreateMockParameter(ParameterInfo parameter, int index)
        {
            if (parameter.ParameterType == typeof(TypeSymbol))
            {
                return new PrimitiveType($"<type_{index}>", TypeSymbolValidationFlags.Default);
            }

            if (parameter.ParameterType == typeof(IList<TypeSymbol>))
            {
                return new List<TypeSymbol> {new PrimitiveType($"<list_type_{index}>", TypeSymbolValidationFlags.Default)};
            }

            if (parameter.ParameterType == typeof(IEnumerable<string>))
            {
                return new List<string> {$"<value_{index}"};
            }

            if (parameter.ParameterType == typeof(IList<string>))
            {
                return new List<string> {$"<value_{index}"};
            }

            if (parameter.ParameterType == typeof(int) || parameter.ParameterType == typeof(int?))
            {
                return 0;
            }

            if (parameter.ParameterType == typeof(bool) || parameter.ParameterType == typeof(bool?))
            {
                return false;
            }

            if (parameter.ParameterType == typeof(SymbolKind))
            {
                return SymbolKind.Variable;
            }

            if (parameter.ParameterType == typeof(ResourceTypeReference))
            {
                return ResourceTypeReference.Parse("Mock.ErrorParam/mockResources@2020-01-01");
            }

            return $"<param_{index}>";
        }
    }
}
