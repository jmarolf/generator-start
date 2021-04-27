using Microsoft.CodeAnalysis;
using System.IO;

namespace WebAssembly.Generators
{
	[Generator]
	internal sealed class WasmTypeGenerator
		: ISourceGenerator
	{
		private const string ClassNameOption = "build_metadata.additionalfiles.ClassName";

		public void Execute(GeneratorExecutionContext context)
		{
			foreach (var additionalText in context.AdditionalFiles)
			{
				if (Path.GetExtension(additionalText.Path).ToLower() == ".wasm")
				{
					if(!context.AnalyzerConfigOptions.GetOptions(additionalText)
						.TryGetValue(WasmTypeGenerator.ClassNameOption, out var className))
					{
						className = Path.GetFileNameWithoutExtension(additionalText.Path);
					}

					var text = WasmTypeBuilder.Build(additionalText.Path, className);
					context.AddSource($"{className}.g.cs", text);
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context) { }
	}
}