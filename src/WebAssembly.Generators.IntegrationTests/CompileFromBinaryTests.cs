using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using NUnit.Framework;

using WebAssembly.Instructions;

namespace WebAssembly.Generators.IntegrationTests
{
    using GeneratorTest = CSharpSourceGeneratorTest<WasmTypeGenerator, NUnitVerifier>;

    public static partial class CompileFromBinaryTests
    {
        [Test]
        public static async Task TestCodeFileIsGenerateAsync()
        {
            var className = $"C_{Guid.NewGuid():N}";
            var fileName = $"{className}.wasm";
            var expectedFileName = $@"{className}.g.cs";
            var expectedContent = $@"public abstract class {className}
{{
	public abstract int Test(int a0);
}}
";
            try
            {
                CreateWASMFile(fileName);
                await new GeneratorTest
                {
                    ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                    TestState =
                    {
                        AdditionalFilesFactories =
                        {
                            () => new []{ (fileName, MySourceText.Instance) }
                        },
                        GeneratedSources =
                        {
                            (typeof(WasmTypeGenerator), expectedFileName, expectedContent)
                        },
                    },
                }.RunAsync();
            }
            finally
            {
                File.Delete(fileName);
            }

            return;

            static void CreateWASMFile(string fileName)
            {
                var module = new Module();
                module.Types.Add(new() { Parameters = new[] { WebAssemblyValueType.Int32 }, Returns = new[] { WebAssemblyValueType.Int32 } });
                module.Functions.Add(new() { Type = 0 });
                module.Codes.Add(new()
                {
                    Code = new Instruction[]
                    {
                    new LocalGet(0),
                    new End()
                    }
                });
                module.Exports.Add(new() { Index = 0, Kind = ExternalKind.Function, Name = "Test" });

                using var writer = new StreamWriter(fileName);
                module.WriteToBinary(writer.BaseStream);
            }
        }
    }
}