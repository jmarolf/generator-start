using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Text;
using System;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Text;

namespace WebAssembly.Generators.IntegrationTests
{
    public static partial class CompileFromBinaryTests
    {
        internal sealed class MySourceText : SourceText
        {
            public static SourceText Instance => new MySourceText();

            public override char this[int position] =>throw new NotImplementedException();

            public override Encoding? Encoding { get; }
            public override int Length { get; }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
            }
        }

    }
}