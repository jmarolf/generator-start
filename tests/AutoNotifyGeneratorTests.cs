using System.Threading.Tasks;
using Xunit;
using Generator;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.IO;

namespace Test
{
    using GeneratorTest = CSharpSourceGeneratorTest<AutoNotifyGenerator, XUnitVerifier>;

    public class AutoNotifyGeneratorTests
    {
        [Fact]
        public async Task TestCodeFileIsGenerated()
        {
            var codeFile = SourceText.From(@"
using System;
using AutoNotify;

namespace GeneratedDemo
{
    // The view model we'd like to augment
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string _text = ""private field text"";

        [AutoNotify(PropertyName = ""Count"")]
        private int _amount = 5;
    }

    public static class UseAutoNotifyGenerator
    {
        public static void Run()
        {
            ExampleViewModel vm = new ExampleViewModel();

            // we didn't explicitly create the 'Text' property, it was generated for us 
            string text = vm.Text;
            Console.WriteLine($""Text = {text}"");

            // Properties can have differnt names generated based on the PropertyName argument of the attribute
            int count = vm.Count;
            Console.WriteLine($""Count = {count}"");

            // the viewmodel will automatically implement INotifyPropertyChanged
            vm.PropertyChanged += (o, e) => Console.WriteLine($""Property {e.PropertyName} was changed"");
            vm.Text = ""abc"";
            vm.Count = 123;

            // Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
            // You'll see the matching generated properties visibile in IntelliSense in realtime
        }
    }
}
", Encoding.UTF8);
            var generatedCode = SourceText.From(@"
namespace GeneratedDemo
{
    public partial class ExampleViewModel : System.ComponentModel.INotifyPropertyChanged
    {
public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
public string Text 
{
    get 
    {
        return this._text;
    }
    set
    {
        this._text = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Text)));
    }
}

public int Count 
{
    get 
    {
        return this._amount;
    }
    set
    {
        this._amount = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
    }
}
} }", Encoding.UTF8);
            var attributeText = SourceText.From(@"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
", Encoding.UTF8);

            var attributeFilePath = Path.Combine("Generator","Generator.AutoNotifyGenerator","AutoNotifyAttribute.cs");
            var generatedCodeFilePath = Path.Combine("Generator","Generator.AutoNotifyGenerator", "ExampleViewModel_autoNotify.cs");
            await new GeneratorTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestState =
                {
                    // We expect there to be compile errors before the generator runs
                    ExpectedDiagnostics =
                    {
                        DiagnosticResult.CompilerError("CS0246").WithSpan(3, 7, 3, 17).WithArguments("AutoNotify"),
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 10, 10, 20).WithArguments("AutoNotify"),
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 10, 10, 20).WithArguments("AutoNotifyAttribute"),
                        DiagnosticResult.CompilerError("CS0246").WithSpan(13, 10, 13, 20).WithArguments("AutoNotify"),
                        DiagnosticResult.CompilerError("CS0246").WithSpan(13, 10, 13, 20).WithArguments("AutoNotifyAttribute"),
                        DiagnosticResult.CompilerError("CS0246").WithSpan(13, 21, 13, 33).WithArguments("PropertyName"),
                        DiagnosticResult.CompilerError("CS1061").WithSpan(24, 30, 24, 34).WithArguments("GeneratedDemo.ExampleViewModel", "Text"),
                        DiagnosticResult.CompilerError("CS1061").WithSpan(28, 28, 28, 33).WithArguments("GeneratedDemo.ExampleViewModel", "Count"),
                        DiagnosticResult.CompilerError("CS1061").WithSpan(32, 16, 32, 31).WithArguments("GeneratedDemo.ExampleViewModel", "PropertyChanged"),
                        DiagnosticResult.CompilerError("CS1061").WithSpan(33, 16, 33, 20).WithArguments("GeneratedDemo.ExampleViewModel", "Text"),
                        DiagnosticResult.CompilerError("CS1061").WithSpan(34, 16, 34, 21).WithArguments("GeneratedDemo.ExampleViewModel", "Count"),
                    },
                    Sources = { codeFile },
                },
                FixedState =
                {
                    // We set this mode so the 'expected diagnostics' value isn't from our inital test state
                    InheritanceMode = StateInheritanceMode.Explicit,
                    // We expect there to be no compile error after the generator runs
                    ExpectedDiagnostics = { },
                    Sources =
                    {
                        codeFile,
                        (attributeFilePath, attributeText),
                        (generatedCodeFilePath, generatedCode), 
                    }
                }
            }.RunAsync();
        }
    }
}
