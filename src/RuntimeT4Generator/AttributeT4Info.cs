using Microsoft.CodeAnalysis.Text;

namespace RuntimeT4Generator;

public sealed record AttributeT4Info(Kind Kind, INamedTypeSymbol TypeSymbol, string ParameterName, SourceText SourceText, string? IndentParameterName)
{
    public static AttributeT4Info? Select((INamedTypeSymbol, (INamedTypeSymbol?, string)) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var (type, (attribute, dir)) = pair;
        if (attribute is null)
        {
            return null;
        }

        foreach (var attributeData in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute))
            {
                token.ThrowIfCancellationRequested();
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

            var file = File.ReadAllBytes(path);
            var sourceText = SourceText.From(file, file.Length, Encoding.UTF8);
            var indentParameterName = isIndent ? "indent" : null;
            return new AttributeT4Info((Kind)kind, type, "builder", sourceText, indentParameterName);
        }

        return null;
    }

    public string HintName => TypeSymbol.ToDisplayString() + "." + ParameterName;
}
