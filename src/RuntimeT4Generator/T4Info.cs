using System.IO;

namespace RuntimeT4Generator;

public partial class T4Info
{
    public static IEnumerable<T4Info> Select(((AdditionalText, AnalyzerConfigOptionsProvider), Options) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var ((text, provider), options) = pair;
        var path = text.Path;
        if (Path.GetExtension(path) != ".tt")
        {
            return Array.Empty<T4Info>();
        }

        var value = new T4Info(text, provider.GetOptions(text));
        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_Namespace))
        {
            if (string.IsNullOrWhiteSpace(options.RootNamespace))
            {
                value.Namespace = "RuntimeT4Generator";
            }
            else
            {
                value.Namespace = options.RootNamespace!;
            }
        }
        else
        {
            value.Namespace = value.RuntimeT4Generator_Namespace!;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_Modifier))
        {
            value.Modifier = "public partial class";
        }
        else
        {
            value.Modifier = value.RuntimeT4Generator_Modifier!;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_TypeName))
        {
            value.TypeName = Path.GetFileNameWithoutExtension(path);
        }
        else
        {
            value.TypeName = value.RuntimeT4Generator_TypeName!;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_ParameterName))
        {
            value.ParameterName = "builder";
        }
        else
        {
            value.ParameterName = value.RuntimeT4Generator_ParameterName!;
        }

        value.MethodPrefix = value.ParameterName + ".Append(";
        switch (value.RuntimeT4Generator)
        {
            case null:
            case "":
            case "StringBuilder":
                return new[] { value.Clone("StringBuilder") };
            case "Utf16":
                return new[] { value.Clone("Utf16") };
            case "Utf8":
                return new[] { value.Clone("Utf8") };
            case "DefaultInterpolatedStringHandler":
                return new[] { value.Clone("DefaultInterpolatedStringHandler") };
        }

        var splits = value.RuntimeT4Generator.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<T4Info>();
        foreach (var line in splits)
        {
            var split = line.AsSpan().Trim();
            if (split.SequenceEqual("StringBuilder".AsSpan()))
            {
                list.Add(value.Clone("StringBuilder"));
            }
            else if (split.SequenceEqual("Utf16".AsSpan()))
            {
                list.Add(value.Clone("Utf16"));
            }
            else if (split.SequenceEqual("Utf8".AsSpan()))
            {
                list.Add(value.Clone("Utf8"));
            }
            else if (split.SequenceEqual("DefaultInterpolatedStringHandler".AsSpan()))
            {
                list.Add(value.Clone("DefaultInterpolatedStringHandler"));
            }
        }

        if (list.Count > 0)
        {
            return list;
        }

        return Array.Empty<T4Info>();
    }

    public T4Info Clone(string runtimeT4Generator)
    {
        return runtimeT4Generator switch
        {
            "StringBuilder" => new(runtimeT4Generator, Text, Namespace, Modifier, "global::System.Text.StringBuilder", RuntimeT4Generator_IndentParameterName, ParameterName, TypeName, MethodPrefix, ParameterName + ".Append(", ParameterName + ".AppendLine();"),
            "Utf16" => new(runtimeT4Generator, Text, Namespace, Modifier, "ref global::Cysharp.Text.Utf16ValueStringBuilder", RuntimeT4Generator_IndentParameterName, ParameterName, TypeName, MethodPrefix, ParameterName + ".Append(", ParameterName + ".AppendLine();"),
            "Utf8" => new(runtimeT4Generator, Text, Namespace, Modifier, "ref global::Cysharp.Text.Utf8ValueStringBuilder", RuntimeT4Generator_IndentParameterName, ParameterName, TypeName, MethodPrefix, ParameterName + "AppendLiteral(", ParameterName + ".AppendLine();"),
            "DefaultInterpolatedStringHandler" => new(runtimeT4Generator, Text, Namespace, Modifier, "ref global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", RuntimeT4Generator_IndentParameterName, ParameterName, TypeName, ParameterName + ".AppendFormatted(", ParameterName + ".AppendFormatted(", ParameterName + ".AppendLiteral(global::System.Environment.NewLine);"),
            _ => throw new ArgumentException(runtimeT4Generator),
        };
    }

    private T4Info(string runtimeT4Generator, AdditionalText text, string @namespace, string modifier, string parameterType, string? indentParameterName, string parameterName, string typeName, string methodPrefix, string methodLiteralPrefix, string methodCrLf)
    {
        RuntimeT4Generator = runtimeT4Generator;
        Namespace = @namespace;
        Modifier = modifier;
        TypeName = typeName;
        ParameterType = parameterType;
        ParameterName = parameterName;
        MethodPrefix = methodPrefix;
        MethodLiteralPrefix = methodLiteralPrefix;
        MethodCrLf = methodCrLf;
        Text = text;
        RuntimeT4Generator_IndentParameterName = indentParameterName;
    }

    public string Namespace;
    public string Modifier;
    public string TypeName;
    public string ParameterType;
    public string ParameterName;
    public string MethodPrefix;
    public string MethodLiteralPrefix;
    public string MethodCrLf;

    public sealed class Comparer : IEqualityComparer<T4Info>
    {
        public bool Equals(T4Info x, T4Info y) => x.RuntimeT4Generator == y.RuntimeT4Generator && x.Modifier == y.Modifier && x.TypeName == y.TypeName && x.ParameterName == y.ParameterName;

        public int GetHashCode(T4Info obj) => obj.RuntimeT4Generator?.GetHashCode() ?? 0;

        public static readonly Comparer Default = new();
    }
}
