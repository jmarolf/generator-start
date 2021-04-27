using NUnit.Framework;
using System;
using System.IO;
using WebAssembly.Instructions;

namespace WebAssembly.Generators.Tests
{
	public static class WasmTypeBuilderTests
	{
		[Test]
		public static void BuildWithSingleExportSingleParameter()
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

			var fileName = $"{Guid.NewGuid():N}.wasm";
			using(var writer = new StreamWriter(fileName))
			{
				module.WriteToBinary(writer.BaseStream);
			}

			try
			{
				var text = WasmTypeBuilder.Build(fileName, "TestClass");
				Assert.That(text.Lines.Count, Is.GreaterThan(0));
			}
			finally
			{
				File.Delete(fileName);
			}
		}

		[Test]
		public static void BuildWithSingleExportMultipleParameters()
		{
			var module = new Module();
			module.Types.Add(new() { Parameters = new[] { WebAssemblyValueType.Int32, WebAssemblyValueType.Float64 }, Returns = new[] { WebAssemblyValueType.Int32 } });
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

			var fileName = $"{Guid.NewGuid():N}.wasm";
			using (var writer = new StreamWriter(fileName))
			{
				module.WriteToBinary(writer.BaseStream);
			}

			try
			{
				var text = WasmTypeBuilder.Build(fileName, "TestClass");
				Assert.That(text.Lines.Count, Is.GreaterThan(0));
			}
			finally
			{
				File.Delete(fileName);
			}
		}

		[Test]
		public static void BuildWithMultipleExportsSingleParameter()
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
			module.Exports.Add(new() { Index = 0, Kind = ExternalKind.Function, Name = "Test2" });

			var fileName = $"{Guid.NewGuid():N}.wasm";
			using (var writer = new StreamWriter(fileName))
			{
				module.WriteToBinary(writer.BaseStream);
			}

			try
			{
				var text = WasmTypeBuilder.Build(fileName, "TestClass");
				Assert.That(text.Lines.Count, Is.GreaterThan(0));
			}
			finally
			{
				File.Delete(fileName);
			}
		}

		[Test]
		public static void BuildWithMultipleExportsMultipleParameters()
		{
			var module = new Module();
			module.Types.Add(new() { Parameters = new[] { WebAssemblyValueType.Int32, WebAssemblyValueType.Float64 }, Returns = new[] { WebAssemblyValueType.Int32 } });
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
			module.Exports.Add(new() { Index = 0, Kind = ExternalKind.Function, Name = "Test2" });

			var fileName = $"{Guid.NewGuid():N}.wasm";
			using (var writer = new StreamWriter(fileName))
			{
				module.WriteToBinary(writer.BaseStream);
			}

			try
			{
				var text = WasmTypeBuilder.Build(fileName, "TestClass");
				Assert.That(text.Lines.Count, Is.GreaterThan(0));
			}
			finally
			{
				File.Delete(fileName);
			}
		}
	}
}