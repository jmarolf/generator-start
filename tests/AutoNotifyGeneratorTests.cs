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
            // We expect there to be compile errors before the generator runs
            // Note how we have annotated where we expect these errors to happen with the {|ErrorId:|} syntax
            const string codeFile = @"
using System;
using {|CS0246:AutoNotify|};

namespace GeneratedDemo
{
    // The view model we'd like to augment
    public partial class ExampleViewModel
    {
        [{|CS0246:{|CS0246:AutoNotify|}|}]
        private string _text = ""private field text"";

        [{|CS0246:{|CS0246:AutoNotify|}|}({|CS0246:PropertyName|} = ""Count"")]
        private int _amount = 5;
    }

    public static class UseAutoNotifyGenerator
    {
        public static void Run()
        {
            ExampleViewModel vm = new ExampleViewModel();

            // we didn't explicitly create the 'Text' property, it was generated for us 
            string text = vm.{|CS1061:Text|};
            Console.WriteLine($""Text = {text}"");

            // Properties can have differnt names generated based on the PropertyName argument of the attribute
            int count = vm.{|CS1061:Count|};
            Console.WriteLine($""Count = {count}"");

            // the viewmodel will automatically implement INotifyPropertyChanged
            vm.{|CS1061:PropertyChanged|} += (o, e) => Console.WriteLine($""Property {e.PropertyName} was changed"");
            vm.{|CS1061:Text|} = ""abc"";
            vm.{|CS1061:Count|} = 123;

            // Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
            // You'll see the matching generated properties visibile in IntelliSense in realtime
        }
    }
}
";
            const string generatedCode = @"
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
} }";
            const string attributeText = @"
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
";

            await new GeneratorTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestState =
                {
                    Sources = { codeFile },
                    GeneratedSources =
                    {
                        (typeof(AutoNotifyGenerator), "AutoNotifyAttribute.cs", attributeText),
                        (typeof(AutoNotifyGenerator), "ExampleViewModel_autoNotify.cs", generatedCode),
                    },
                },
            }.RunAsync();
        }
    }
}
