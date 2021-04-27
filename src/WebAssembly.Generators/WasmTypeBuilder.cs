using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebAssembly.Generators.Extensions;

namespace WebAssembly.Generators
{
	internal static class WasmTypeBuilder
	{
		internal static SourceText Build(string wasmPath, string className)
		{
			var module = Module.ReadFromBinary(wasmPath);

			using var writer = new StringWriter();
			using var indentWriter = new IndentedTextWriter(writer, "\t");

			indentWriter.WriteLine($"public abstract class {className}");
			indentWriter.WriteLine("{");
			WasmTypeBuilder.BuildMethods(module, indentWriter);
			indentWriter.WriteLine("}");

			// TODO: Should we have some way to create a "GetImports"?

			return SourceText.From(writer.ToString(), Encoding.UTF8);
		}

		private static void BuildMethods(Module module, IndentedTextWriter writer)
		{
			static IEnumerable<string> GetParameters(IList<WebAssemblyValueType> parameters)
			{
				var count = 0;

				foreach (var parameter in parameters)
				{
					yield return $"{parameter.GetCSharpName()} a{count}";
					count++;
				}
			}

			var exports = module.Exports.Where(_ => _.Kind == ExternalKind.Function).ToList();

			for(var i = 0; i < exports.Count; i++)
			{
				var export = exports[i];
				var function = module.Functions[(int)export.Index];
				var type = module.Types[(int)function.Type];
				var parameters = string.Join(", ", GetParameters(type.Parameters));
				writer.Indent++;
				writer.WriteLine($"public abstract {type.Returns[0].GetCSharpName()} {export.Name}({parameters});");
				writer.Indent--;

				if(i < exports.Count - 1)
				{
					writer.WriteLine();
				}
			}
		}
	}
}