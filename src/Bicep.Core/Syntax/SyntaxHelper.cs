// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Linq;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.Syntax
{
    public static class SyntaxHelper
    {
        private static SyntaxBase? TryGetObjectProperty(ObjectSyntax objectSyntax, string propertyName)
            => objectSyntax.Properties.SingleOrDefault(p => p.TryGetKeyText() == propertyName)?.Value;

        public static IEnumerable<SyntaxBase>? TryGetAllowedItems(ParameterDeclarationSyntax parameterDeclarationSyntax)
        {
            if (!(parameterDeclarationSyntax.Modifier is ObjectSyntax modifierObject))
            {
                return null;
            }

            var allowedValuesSyntax = TryGetObjectProperty(modifierObject, LanguageConstants.ParameterAllowedPropertyName);
            if (!(allowedValuesSyntax is ArraySyntax allowedArraySyntax))
            {
                return null;
            }

            return allowedArraySyntax.Items.Select(i => i.Value);
        }

        public static SyntaxBase? TryGetDefaultValue(ParameterDeclarationSyntax parameterDeclarationSyntax)
        {
            if (parameterDeclarationSyntax.Modifier is ParameterDefaultValueSyntax defaultValueSyntax)
            {
                return defaultValueSyntax.DefaultValue;
            }

            if (parameterDeclarationSyntax.Modifier is ObjectSyntax modifierObject)
            {
                return TryGetObjectProperty(modifierObject, LanguageConstants.ParameterDefaultPropertyName);
            }

            return null;
        }

        public static string? TryGetModulePath(ModuleDeclarationSyntax moduleDeclarationSyntax)
        {
            var pathSyntax = moduleDeclarationSyntax.TryGetPath();
            if (pathSyntax == null)
            {
                return null;
            }
            
            return pathSyntax?.TryGetLiteralValue();
        }

        public static TypeSymbol? TryGetPrimitiveType(ParameterDeclarationSyntax parameterDeclarationSyntax)
            => LanguageConstants.TryGetDeclarationType(parameterDeclarationSyntax.ParameterType?.TypeName);
    }
}