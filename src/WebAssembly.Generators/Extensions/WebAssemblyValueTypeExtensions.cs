using System.ComponentModel;

namespace WebAssembly.Generators.Extensions
{
	internal static class WebAssemblyValueTypeExtensions
	{
		internal static string GetCSharpName(this WebAssemblyValueType self) =>
			self switch
			{
				WebAssemblyValueType.Float32 => "single",
				WebAssemblyValueType.Float64 => "double",
				WebAssemblyValueType.Int32 => "int",
				WebAssemblyValueType.Int64 => "long",
				_ => throw new InvalidEnumArgumentException(nameof(self), (int)self, typeof(WebAssemblyValueType))
			};
	}
}