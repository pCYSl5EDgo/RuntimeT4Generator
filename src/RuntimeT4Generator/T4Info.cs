using System.IO;

namespace RuntimeT4Generator;

public partial class T4Info
{
    public static T4Info? Select(((AdditionalText, AnalyzerConfigOptionsProvider), Options) pair, CancellationToken token)
    {
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

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_ParameterType))
        {
            value.ParameterType = value.RuntimeT4Generator switch
            {
                null or "" or "StringBuilder" => "global::System.Text.StringBuilder",
                _ => null!
            };
        }
        else
        {
            value.ParameterType = value.RuntimeT4Generator_ParameterType!;
        }

        if (string.IsNullOrWhiteSpace(value.ParameterType))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_MethodPrefix))
        {
            value.MethodPrefix = value.RuntimeT4Generator switch
            {
                null or "" or "StringBuilder" => value.ParameterName + ".Append(",
                _ => null!
            };
        }
        else
        {
            value.MethodPrefix = value.RuntimeT4Generator_MethodPrefix!;
        }

        if (string.IsNullOrWhiteSpace(value.MethodPrefix))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_MethodSuffix))
        {
            value.MethodSuffix = value.RuntimeT4Generator switch
            {
                null or "" or "StringBuilder" => ");",
                _ => null!
            };
        }
        else
        {
            value.MethodSuffix = value.RuntimeT4Generator_MethodSuffix!;
        }

        if (string.IsNullOrWhiteSpace(value.MethodSuffix))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_MethodLiteralPrefix))
        {
            value.MethodLiteralPrefix = value.RuntimeT4Generator switch
            {
                null or "" or "StringBuilder" => value.ParameterName + ".Append(",
                _ => null!
            };
        }
        else
        {
            value.MethodLiteralPrefix = value.RuntimeT4Generator_MethodLiteralPrefix!;
        }

        if (string.IsNullOrWhiteSpace(value.MethodLiteralPrefix))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeT4Generator_MethodLiteralSuffix))
        {
            value.MethodLiteralSuffix = value.RuntimeT4Generator switch
            {
                null or "" or "StringBuilder" => ");",
                _ => null!
            };
        }
        else
        {
            value.MethodLiteralSuffix = value.RuntimeT4Generator_MethodLiteralSuffix!;
        }
        

        if (string.IsNullOrWhiteSpace(value.MethodLiteralSuffix))
        {
            return null;
        }

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
