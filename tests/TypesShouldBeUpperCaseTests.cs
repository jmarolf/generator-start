using System.Threading.Tasks;
using Analyzer;
using CodeFix;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;


namespace Test
{
    using VerifyCS = CSharpCodeFixVerifier<
        TypesShouldBeUpperCaseAnalyzer,
        TypesShouldBeUpperCaseFixer,
        XUnitVerifier>;

    public class TypesShouldBeUpperCaseTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task TestEmptyFile()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [Fact]
        public async Task TestTypeNameIsLowerCase()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    namespace ConsoleApplication1
    {
        class TYPENAME
        {
        }
    }";

            var expected = VerifyCS
                .Diagnostic(Analyzer.TypesShouldBeUpperCaseAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
