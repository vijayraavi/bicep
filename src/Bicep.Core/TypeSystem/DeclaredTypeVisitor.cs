using Bicep.Core.Syntax;

namespace Bicep.Core.TypeSystem
{
    public class DeclaredTypeVisitor
    {
        private readonly SyntaxHierarchy hierarchy;

        public DeclaredTypeVisitor(SyntaxHierarchy hierarchy)
        {
            this.hierarchy = hierarchy;
        }


    }

    public static class DeclaredType
    {
        public static TypeSymbol? GetDeclaredType(SyntaxBase syntax)
        {


        }

        public static TypeSymbol? GetDeclaredType(ParameterDeclarationSyntax syntax)
        {
            return syntax.ParameterType == null
                ? null
                : LanguageConstants.TryGetDeclarationType(syntax.ParameterType.TypeName);
        }




    }
}