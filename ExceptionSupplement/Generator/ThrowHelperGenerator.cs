using IncrementalSourceGeneratorSupplement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ExceptionSupplement.Generator;

[Generator]
public class ThrowHelperGenerator : IncrementalSourceGeneratorBase<ClassDeclarationSyntax>
{
    private static SymbolDisplayFormat FullNameNoTypeParamAndGlobal { get; } = new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    const string Namespace = nameof(ExceptionSupplement);
    const string AttributeName = "ThrowExceptionAttribute";
    const string AttributeFullName = $"{Namespace}.{AttributeName}";

    protected override ImmutableArray<AttributeData> FilterAttribute(Compilation compilation, ImmutableArray<AttributeData> attributes)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(AttributeFullName) ?? throw new NullReferenceException($"{AttributeFullName} was not found.");
        var filtered = attributes.Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol)).ToImmutableArray();
        return filtered.IsEmpty ? default : filtered;
    }

    protected override (string HintName, SourceText Source) ProductInitialSource()
    {
        var source = SourceText.From(
@"namespace ExceptionSupplement
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
    class ThrowExceptionAttribute : global::System.Attribute
    {
        public ThrowExceptionAttribute(Type type)
        {
            if(type.BaseType != typeof(global::System.Attribute)) throw new global::System.ArgumentException();
            this.ExceptionType = type;
        }

        public Type ExceptionType { get; }
    }
}", Encoding.UTF8);

        return ("ExceptionSupplement.ThrowExceptionAttribute.g.cs", source);
    }
    protected override (string HintName, SourceText Source) ProductSource(Compilation compilation, ISymbol symbol, ImmutableArray<AttributeData> attributes)
    {
        try
        {
            var writer = new IndentedWriter("    ");

            if (!symbol.ContainingNamespace.IsGlobalNamespace)
            {
                writer["namespace "][symbol.ContainingNamespace.ToDisplayString(FullNameNoTypeParamAndGlobal)].Line()
                      ["{"].Line().Indent(1);
            }

            if (symbol.IsStatic)
            {
                writer["static "].End();
            }
            writer["partial class "][symbol.Name].Line()
                  ["{"].Line()
                  .Indent(+1);

            foreach (var attributeData in attributes)
            {
                var attribute = (attributeData.ConstructorArguments.First().Value as INamedTypeSymbol)!;
                var constructors = attribute.Constructors;
                const string ExceptionSufix = "Exception";


                var signeture = RemoveSufix(attribute.Name, ExceptionSufix);

                foreach (var constructor in constructors.Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public))
                {
                    var parameters = constructor.Parameters;
                    writer["public static void Throw"][signeture]["("].End();

                    for (var index = 0; index < parameters.Length; ++index)
                    {
                        var parameter = parameters[index];
                        writer[parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)][" "][parameter.Name].End();
                        if (index + 1 < parameters.Length) writer[", "].End();
                    }
                    writer[")"].Line()
                          ["{"].Line().Indent(1);

                    writer["throw new "][attribute.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)]["("].End();
                    for (var index = 0; index < parameters.Length; ++index)
                    {
                        var parameter = parameters[index];
                        writer[parameter.Name].End();
                        if (index + 1 < parameters.Length) writer[", "].End();
                    }
                    writer[");"].Line().Indent(-1)["}"].Line();
                }
            }

            writer.Indent(-1)["}"].Line();

            if (!symbol.ContainingNamespace.IsGlobalNamespace)
            {
                writer.Indent(-1)["}"].Line();
            }

            return ($"{symbol.ToDisplayString(FullNameNoTypeParamAndGlobal)}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));


            static string RemoveSufix(string str, string sufix)
            {
                if (string.IsNullOrEmpty(sufix)) return str;
                if (!str.EndsWith(sufix, StringComparison.Ordinal)) return str;
                return str.Remove(str.Length - sufix.Length, sufix.Length);
            }
        }
        catch (Exception ex)
        {
            throw new AggregateException($"{ex.GetType()} was thrown. message : {ex.Message} | stack trace : {ex.StackTrace}", ex);
        }
    }
}