using Microsoft.CodeAnalysis.Text;

namespace RuntimeT4Generator;

public sealed record AdditionalFilesT4Info(Kind Kind, string? Namespace, string Modifier, string TypeName, string ParameterName, SourceText SourceText, string? IndentParameterName)
{
    public static AdditionalFilesT4Info? Select((T4Info, string?) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var (info, rootNamespace) = pair;
        Kind kind = 0;
        switch (info.RuntimeT4Generator)
        {
            case "":
            case null:
            case nameof(Kind.StringBuilder):
                kind |= Kind.StringBuilder;
                break;
            case nameof(Kind.Utf8):
                kind |= Kind.Utf8;
                break;
            case nameof(Kind.Utf16):
                kind |= Kind.Utf16;
                break;
            case nameof(Kind.DefaultInterpolatedStringHandler):
                kind |= Kind.DefaultInterpolatedStringHandler;
                break;
            default:
                var span = info.RuntimeT4Generator.AsSpan().Trim();
                while (!span.IsEmpty)
                {
                    if (span.StartsWith(nameof(Kind.StringBuilder).AsSpan()))
                    {
                        kind |= Kind.StringBuilder;
                        span = span.Slice(nameof(Kind.StringBuilder).Length);
                    }
                    else if (span.StartsWith(nameof(Kind.Utf8).AsSpan()))
                    {
                        kind |= Kind.Utf8;
                        span = span.Slice(nameof(Kind.Utf8).Length);
                    }
                    else if (span.StartsWith(nameof(Kind.Utf16).AsSpan()))
                    {
                        kind |= Kind.Utf16;
                        span = span.Slice(nameof(Kind.Utf16).Length);
                    }
                    else if (span.StartsWith(nameof(Kind.DefaultInterpolatedStringHandler).AsSpan()))
                    {
                        kind |= Kind.DefaultInterpolatedStringHandler;
                        span = span.Slice(nameof(Kind.DefaultInterpolatedStringHandler).Length);
                    }
                    else
                    {
                        return null;
                    }

                    span = span.Slice(span.IndexOf(',') + 1).TrimStart();
                }
                break;
        }

        if (kind == 0)
        {
            return null;
        }

        var sourceText = info.Text.GetText(token);
        if (sourceText is null)
        {
            return null;
        }

        var modifier = info.RuntimeT4Generator_Modifier;
        if (string.IsNullOrWhiteSpace(modifier))
        {
            modifier = "partial class";
        }

        var typeName = info.RuntimeT4Generator_TypeName;
        if (string.IsNullOrWhiteSpace(typeName))
        {
            typeName = Path.GetFileNameWithoutExtension(info.Text.Path);
        }

        var parameterName = info.RuntimeT4Generator_ParameterName;
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            parameterName = "builder";
        }

        var indentParameterName = info.RuntimeT4Generator_IndentParameterName;
        if (string.IsNullOrWhiteSpace(indentParameterName))
        {
            indentParameterName = null;
        }

        return new AdditionalFilesT4Info(kind, info.RuntimeT4Generator_Namespace ?? rootNamespace, modifier!, typeName!, parameterName!, sourceText, indentParameterName);
    }

    public int Preprocess(StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Namespace))
        {
            return 0;
        }
        
        builder.Append("namespace ");
        builder.AppendLine(Namespace);
        builder.AppendLine("{");
        return 4;
    }

    public void Postprocess(StringBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(Namespace))
        {
            builder.AppendLine("}");
        }
    }
}
