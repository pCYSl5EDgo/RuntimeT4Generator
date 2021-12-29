using System.IO;

namespace RuntimeT4Generator;

public partial class T4Info
{
    public static T4Info? Select(((AdditionalText, AnalyzerConfigOptionsProvider), Options) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var ((text, provider), options) = pair;
        var path = text.Path;
        if (Path.GetExtension(path) != ".tt")
        {
            return null;
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

        switch (value.RuntimeT4Generator)
        {
            case null:
            case "":
                value.RuntimeT4Generator = "StringBuilder";
                goto case "StringBuilder";
            case "StringBuilder":
                value.ParameterType = "global::System.Text.StringBuilder";
                value.MethodLiteralPrefix = value.ParameterName + ".Append(";
                break;
            case "Utf16":
                value.ParameterType = "ref global::Cysharp.Text.Utf16ValueStringBuilder";
                value.MethodLiteralPrefix = value.ParameterName + ".Append(";
                break;
            case "Utf8":
                value.ParameterType = "ref global::Cysharp.Text.Utf8ValueStringBuilder";
                value.MethodLiteralPrefix = "CopyTo(ref " + value.ParameterName + ", ";
                break;
            default:
                return null;
        }
        
        value.MethodPrefix = value.ParameterName + ".Append(";
        value.MethodLiteralSuffix = value.MethodSuffix = ");";
        return value;
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
