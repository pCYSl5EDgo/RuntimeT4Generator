using System.IO;
using System.Text;

namespace RuntimeT4Generator;

partial class Utility
{
    internal const string Attribute = @"// <auto-generated />

namespace RuntimeT4Generator
{
    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false, Inherited=false)]
    internal sealed class T4Attribute : global::System.Attribute
    {
        public T4Attribute(string path = """", Kind kind = Kind.StringBuilder, bool isIndent = false)
        {
        }
    }

    [global::System.FlagsAttribute]
    internal enum Kind
    {
        StringBuilder = 1,
        Utf8 = 2,
        Utf16 = 4,
        DefaultInterpolatedStringHandler = 8,
    }
}
";

    public static INamedTypeSymbol? SelectT4Attribute(Compilation compilation, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var type = compilation.GetTypeByMetadataName("RuntimeT4Generator.T4Attribute");
        return type;
    }

    public static bool Predicate(SyntaxNode node, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return node is TypeDeclarationSyntax { AttributeLists.Count: > 0, TypeParameterList: null };
    }

    public static INamedTypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (context.Node is not TypeDeclarationSyntax typeDeclarationSyntax)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax, token);
    }

    internal static IEnumerable<T4Info> TransformToT4Info((INamedTypeSymbol, (INamedTypeSymbol?, string?)) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var (type, (attribute, dir)) = pair;
        if (attribute is null || dir is null)
        {
            return Array.Empty<T4Info>();
        }

        foreach (var attributeData in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute))
            {
                continue;
            }

            var arguments = attributeData.ConstructorArguments;
            if (arguments.Length != 3)
            {
                break;
            }

            if (arguments[0].Value is not string path)
            {
                break;
            }

            if (arguments[1].Value is not int kind || kind >= 16 || kind < 0)
            {
                break;
            }

            if (arguments[2].Value is not bool isIndent)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(dir, type.Name + ".tt");
            }
            else if (path.EndsWith(".tt"))
            {
                path = Path.Combine(dir, path);
            }
            else
            {
                path = Path.Combine(dir, path, type.Name + ".tt");
            }

            if (!File.Exists(path))
            {
                break;
            }

            var sourceText = File.ReadAllText(path, Encoding.UTF8);
            var @namespace = type.ContainingNamespace.ToDisplayString();
            var modifier = type.IsValueType ? "partial struct " : "partial class ";
            var indentParameterName = isIndent ? "indent" : null;
            var list = new List<T4Info>(4);
            if ((kind & 1) == 1)
            {
                list.Add(new("StringBuilder", sourceText, @namespace, modifier, "global::System.Text.StringBuilder", indentParameterName, "builder", type.Name, "builder.Append(", "builder.Append(", "builder.AppendLine();"));
            }

            if ((kind & 2) == 2)
            {
                list.Add(new("Utf8", sourceText, @namespace, modifier, "ref global::Cysharp.Text.Utf8ValueStringBuilder", indentParameterName, "builder", type.Name, "builder.Append(", "builder.Append(", "builder.AppendLine();"));
            }

            if ((kind & 4) == 4)
            {
                list.Add(new("Utf16", sourceText, @namespace, modifier, "ref global::Cysharp.Text.Utf16ValueStringBuilder", indentParameterName, "builder", type.Name, "builder.Append(", "builder.Append(", "builder.AppendLine();"));
            }

            if ((kind & 8) == 8)
            {
                list.Add(new("DefaultInterpolatedStringHandler", sourceText, @namespace, modifier, "ref global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", indentParameterName, "builder", type.Name, "builder.AppendFormatted(", "builder.AppendFormatted(", "builder.AppendLiteral(global::System.Environment.NewLine);"));
            }

            return list.Count == 0 ? Array.Empty<T4Info>() : list;
        }

        return Array.Empty<T4Info>();
    }
}