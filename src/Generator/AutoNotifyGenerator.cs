using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public partial class AutoNotifyGenerator : ISourceGenerator
    {
        private const string AttributeText = @"
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

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("AutoNotifyAttribute", SourceText.From(AttributeText, Encoding.UTF8)));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            // get the added attribute, and INotifyPropertyChanged
            INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
            INamedTypeSymbol? notifySymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            if (attributeSymbol is null || notifySymbol is null)
            {
                return;
            }

            // group the fields by class, and generate the source
            foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                if (group.Key is not INamedTypeSymbol namedType)
                {
                    continue;
                }

                string? classSource = ProcessClass(namedType, group.ToList(), attributeSymbol, notifySymbol);
                if (classSource is not null)
                {
                    context.AddSource($"{group.Key.Name}_autoNotify.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private static string? ProcessClass(INamedTypeSymbol classSymbol,
                                            List<IFieldSymbol> fields,
                                            ISymbol attributeSymbol,
                                            ISymbol notifySymbol)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

            // if the class doesn't implement INotifyPropertyChanged already, add it
            if (!classSymbol.Interfaces.Contains(notifySymbol, SymbolEqualityComparer.Default))
            {
                source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
            }

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            source.Append("} }");
            return source.ToString();
        }

        private static void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string? propertyName = ChooseName(fieldName, overridenNameOpt);
            if (propertyName is { Length: 0 } || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            source.Append($@"
public {fieldType} {propertyName} 
{{
    get 
    {{
        return this.{fieldName};
    }}
    set
    {{
        this.{fieldName} = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));
    }}
}}
");

            return;

            static string? ChooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value?.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName[1..];
            }
        }
    }
}
