using System.IO;

namespace RuntimeT4Generator;

public sealed class T4Info : IEquatable<T4Info>
{
    public readonly string Namespace;
    public readonly string Class;
    public readonly string ParameterType;
    public readonly string ParameterName;
    public readonly string InstanceMethodAsAppend;
    public readonly string Text;

    public T4Info(string @namespace, string @class, string parameterType, string parameterName, string instanceMethodAsAppend, string text)
    {
        Namespace = @namespace;
        Class = @class;
        ParameterType = parameterType;
        ParameterName = parameterName;
        InstanceMethodAsAppend = instanceMethodAsAppend;
        Text = text;
    }

    public static T4Info? Select(AdditionalText text, AnalyzerConfigOptionsProvider provider, Options options)
    {
        var configOptions = provider.GetOptions(text);
        var isRuntimeT4 = configOptions.TryGetValue("build_metadata.AdditionalFiles.RuntimeT4Generator", out _);
        const string prefix = "build_metadata.AdditionalFiles.RuntimeT4Generator_";
        if (configOptions.TryGetValue(prefix + nameof(Namespace), out var @namespace))
        {
            isRuntimeT4 = true;
        }
        else
        {
            @namespace = null;
        }

        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = options.RootNamespace;
        }

        if (string.IsNullOrEmpty(@namespace))
        {
            return null;
        }

        if (configOptions.TryGetValue(prefix + nameof(Class), out var @class))
        {
            isRuntimeT4 = true;
        }
        else
        {
            @class = null;
        }

        if (string.IsNullOrEmpty(@class))
        {
            @class = Path.GetFileNameWithoutExtension(text.Path);
        }

        if (configOptions.TryGetValue(prefix + nameof(ParameterType), out var parameterType))
        {
            isRuntimeT4 = true;
        }
        else
        {
            parameterType = null;
        }

        if (string.IsNullOrEmpty(parameterType))
        {
            parameterType = "global::System.Text.StringBuilder";
        }

        if (configOptions.TryGetValue(prefix + nameof(ParameterName), out var parameterName))
        {
            isRuntimeT4 = true;
        }
        else
        {
            parameterName = null;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            parameterName = "builder";
        }

        if (configOptions.TryGetValue(prefix + nameof(InstanceMethodAsAppend), out var instanceMethodAsAppend))
        {
            isRuntimeT4 = true;
        }
        else
        {
            instanceMethodAsAppend = null;
        }

        if (string.IsNullOrEmpty(instanceMethodAsAppend))
        {
            instanceMethodAsAppend = "Append";
        }

        if (!isRuntimeT4)
        {
            return null;
        }

        var code = text.GetText()?.ToString();
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        return new(@namespace!, @class!, parameterType!, parameterName!, instanceMethodAsAppend!, code!);
    }

    public bool Equals(T4Info other)
    {
        return Namespace == other.Namespace
            && Class == other.Class
            && ParameterType == other.ParameterType
            && ParameterName == other.ParameterName
            && InstanceMethodAsAppend == other.InstanceMethodAsAppend
            && Text == other.Text;
    }
}
