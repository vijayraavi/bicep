// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.SemanticModel;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem.Az;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.TypeSystem
{
    [TestClass]
    public class TypeAssignmentVisitorTests
    {
        [TestMethod]
        public void Foo()
        {
            var program = SyntaxFactory.CreateFromText(@"
param pass string {
  metadata: {
    description: 'test'
  }
}

//resource foo 'Microsoft.Storage/storageAccounts@2019-07-01' = {
//  name: 'test'
//}
");

            var compilation = new Compilation(new AzResourceTypeProvider(), program);
            var model = compilation.GetSemanticModel();

            var diagnostics = model.GetAllDiagnostics();
        }
    }
}
