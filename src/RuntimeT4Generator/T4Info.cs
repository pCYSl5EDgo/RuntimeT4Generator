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

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_Class))
        {
            value.Class = Path.GetFileNameWithoutExtension(path);
        }
        else
        {
            value.Class = value.RuntimeT4Generator_Class!;
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
        value.MethodLiteralSuffix = value.MethodSuffix = ");";

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
        }

        if (list.Count > 0)
        {
            return list;
        }

        return Array.Empty<T4Info>();
    }

    public T4Info Clone(string runtimeT4Generator)
    {
        switch (runtimeT4Generator)
        {
            case "StringBuilder":
                return new(runtimeT4Generator, Namespace, Class, "global::System.Text.StringBuilder", ParameterName, MethodPrefix, MethodSuffix, ParameterName + ".Append(", MethodLiteralSuffix, Text);
            case "Utf16":
                return new(runtimeT4Generator, Namespace, Class, "ref global::Cysharp.Text.Utf16ValueStringBuilder", ParameterName, MethodPrefix, MethodSuffix, ParameterName + ".Append(", MethodLiteralSuffix, Text);
            case "Utf8":
                return new(runtimeT4Generator, Namespace, Class, "ref global::Cysharp.Text.Utf8ValueStringBuilder", ParameterName, MethodPrefix, MethodSuffix, "CopyTo(ref " + ParameterName + ", ", MethodLiteralSuffix, Text);
            default:
                throw new ArgumentException(runtimeT4Generator);
        }
    }

    private T4Info(string runtimeT4Generator, string @namespace, string @class, string parameterType, string parameterName, string methodPrefix, string methodSuffix, string methodLiteralPrefix, string methodLiteralSuffix, AdditionalText text)
    {
        RuntimeT4Generator = runtimeT4Generator;
        Namespace = @namespace;
        Class = @class;
        ParameterType = parameterType;
        ParameterName = parameterName;
        MethodPrefix = methodPrefix;
        MethodSuffix = methodSuffix;
        MethodLiteralPrefix = methodLiteralPrefix;
        MethodLiteralSuffix = methodLiteralSuffix;
        Text = text;
    }

    public string Namespace;
    public string Class;
    public string ParameterType;
    public string ParameterName;
    public string MethodPrefix;
    public string MethodSuffix;
    public string MethodLiteralPrefix;
    public string MethodLiteralSuffix;
}
