using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System.Collections.Immutable;
using System;
using System.Linq;
using System.IO;
using WebAssembly.Instructions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace WebAssembly.Generators.Tests
{
	public static class WasmTypeGeneratorTests
	{
		[Test]
		public static void Create()
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

			var className = $"C_{Guid.NewGuid():N}";
			var fileName = $"{className}.wasm";
			using (var writer = new StreamWriter(fileName))
			{
				module.WriteToBinary(writer.BaseStream);
			}

			try
			{
				var (diagnostics, output) = WasmTypeGeneratorTests.GetGeneratedOutput(fileName);

				Assert.Multiple(() =>
				{
					Assert.That(diagnostics.Length, Is.EqualTo(0));
					Assert.That(output, Does.Contain($"public abstract class {className}"));
					Assert.That(output, Does.Contain("public abstract int Test(int a0);"));
				});
			}
			finally
			{
				File.Delete(fileName);
			}
		}

		private static (ImmutableArray<Diagnostic>, string) GetGeneratedOutput(string wasmPath)
		{
			var references = AppDomain.CurrentDomain.GetAssemblies()
				.Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
				.Select(_ => MetadataReference.CreateFromFile(_.Location))
				.Concat(new[] { MetadataReference.CreateFromFile(typeof(WasmTypeGenerator).Assembly.Location) });
			var compilation = CSharpCompilation.Create("generator",
				references: references,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			var originalTreeCount = compilation.SyntaxTrees.Length;
			var generator = new WasmTypeGenerator();

			var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator), 
				additionalTexts: ImmutableArray.Create<AdditionalText>(new MyAdditionalText(wasmPath)));
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

			var trees = outputCompilation.SyntaxTrees.ToList();

			return (diagnostics, trees.Count != originalTreeCount ? trees[^1].ToString() : string.Empty);
		}
	}

	internal sealed class MyAdditionalText
		: AdditionalText
	{
		internal MyAdditionalText(string path) => this.Path = path; 

		public override string Path { get; }

		public override SourceText? GetText(CancellationToken cancellationToken = default) => throw new NotImplementedException();
	}
}