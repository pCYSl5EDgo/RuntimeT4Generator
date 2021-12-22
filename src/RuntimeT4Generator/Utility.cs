using System.Text;

namespace RuntimeT4Generator;

public static class Utility
{
    public static T4Info? SelectT4File(((AdditionalText, AnalyzerConfigOptionsProvider), Options) pair, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var ((text, provider), options) = pair;
        if (text.Path.EndsWith(".tt"))
        {
            return T4Info.Select(text, provider, options);
        }

        return default;
    }

    public static (string HintName, string Code) Generate(T4Info info, bool isDesignTimeBuild, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        var hintName = builder.Append(info.Namespace).Append('.').Append(info.Class).Append(".g.cs").ToString();
        builder.Clear();
        if (isDesignTimeBuild)
        {
            GenerateDesignTimeBuild(builder, info, token);
        }
        else
        {
            GenerateFull(builder, info, token);
        };

        var code = builder.ToString();
        return (hintName, code);
    }

    private static void GenerateFull(StringBuilder builder, T4Info info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var span = info.Text.AsSpan();
        builder
            .AppendPreprocess(ref span, token)
            .Append("namespace ").AppendLine(info.Namespace)
            .AppendLine("{")
            .Append("    public partial class ").AppendLine(info.Class)
            .AppendLine("    {")
            .Append("        public void TransformAppend(").Append(info.ParameterType).Append(' ').Append(info.ParameterName).AppendLine(")")
            .AppendLine("        {")
            .AppendGenerate(info, span, token)
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}")
            .AppendLine();
    }

    private static StringBuilder AppendPreprocess(this StringBuilder builder, ref ReadOnlySpan<char> text, CancellationToken token)
    {
        ReadOnlySpan<char> specialStart = "<#@".AsSpan();
        ReadOnlySpan<char> end = "#>".AsSpan();

        while (!text.IsEmpty)
        {
            token.ThrowIfCancellationRequested();
            switch (text[0])
            {
                case '\r':
                    if (text.Length >= 2 && text[1] == '\n')
                    {
                        text = text.Slice(2);
                        if (text.IsEmpty)
                        {
                            goto RETURN;
                        }
                    }
                    break;
                case '\n':
                    text = text.Slice(1);
                    if (text.IsEmpty)
                    {
                        goto RETURN;
                    }
                    break;
            }

            if (!text.StartsWith(specialStart))
            {
                break;
            }

            text = text.Slice(specialStart.Length);
            var endIndex = text.IndexOf(end);
            if (endIndex == -1)
            {
                break;
            }

            if (TryGetUsingNamespace(text.Slice(0, endIndex), out var namespaceSpan))
            {
                builder.Append("using ");
                foreach (var c in namespaceSpan)
                {
                    builder.Append(c);
                }

                builder.AppendLine(";");
            }

            text = text.Slice(endIndex + end.Length);
            continue;
        }

    RETURN:
        return builder.AppendLine();
    }

    private static StringBuilder AppendGenerate(this StringBuilder builder, T4Info info, ReadOnlySpan<char> text, CancellationToken token)
    {
        ReadOnlySpan<char> end = "#>".AsSpan();
        ReadOnlySpan<char> assignStart = "<#=".AsSpan();
        ReadOnlySpan<char> normalStart = "<#".AsSpan();
        const string indent3 = "            ";

        while (!text.IsEmpty)
        {
        HEAD:
            switch (text[0])
            {
                case '\r':
                    if (text.Length >= 2 && text[1] == '\n')
                    {
                        text = text.Slice(2);
                        if (text.IsEmpty)
                        {
                            goto RETURN;
                        }
                    }
                    break;
                case '\n':
                    text = text.Slice(1);
                    if (text.IsEmpty)
                    {
                        goto RETURN;
                    }
                    break;
            }

        STEP:
            token.ThrowIfCancellationRequested();
            if (text.StartsWith(assignStart))
            {
                text = text.Slice(assignStart.Length);
                var endIndex = text.IndexOf(end);
                if (endIndex == -1)
                {
                    break;
                }

                builder.Append(indent3).Append(info.ParameterName).Append('.').Append(info.InstanceMethodAsAppend).Append('(');
                foreach (var c in text.Slice(0, endIndex).Trim())
                {
                    builder.Append(c);
                }

                builder.AppendLine(");");
                text = text.Slice(endIndex + end.Length);
                continue;
            }
            else if (text.StartsWith(normalStart))
            {
                text = text.Slice(normalStart.Length);
                for (int i = 0; i < text.Length; i++)
                {
                    var c = text[i];
                    if (c == '#' && text.Length > i + 1 && text[i + 1] == '>')
                    {
                        text = text.Slice(i + 2);
                        goto STEP;
                    }

                    builder.Append(c);
                }

                break;
            }

            builder.Append(indent3).Append(info.ParameterName).Append('.').Append(info.InstanceMethodAsAppend).Append("@\"");
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                switch (c)
                {
                    case '<':
                        if (i + 1 < text.Length && text[i + 1] != '#')
                        {
                            builder.Append('<');
                        }
                        else
                        {
                            builder.AppendLine("\");");
                            text = text.Slice(i);
                            goto HEAD;
                        }
                        break;
                    case '"':
                        builder.Append("\"\"");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            builder.AppendLine("\");");
            break;
        }

    RETURN:
        return builder;
    }

    internal static bool TryGetUsingNamespace(ReadOnlySpan<char> content, out ReadOnlySpan<char> namespaceSpan)
    {
        ReadOnlySpan<char> import = "import".AsSpan();
        ReadOnlySpan<char> namespaceEqualQuotation = "namespace=\"".AsSpan();
        content = content.Trim();
        namespaceSpan = content;

        if (!content.StartsWith(import))
        {
            return false;
        }

        content = content.Slice(import.Length);
        content = content.TrimStart();

        if (!content.StartsWith(namespaceEqualQuotation))
        {
            return false;
        }

        content = content.Slice(namespaceEqualQuotation.Length);
        namespaceSpan = content.Slice(0, content.IndexOf('"'));
        return true;
    }

    private static void GenerateDesignTimeBuild(StringBuilder builder, T4Info info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        builder
            .AppendLine("// DesignTimeBuild")
            .Append("namespace ").AppendLine(info.Namespace)
            .AppendLine("{")
            .Append("    public partial class ").AppendLine(info.Class)
            .AppendLine("    {")
            .Append("        public void TransformAppend(").Append(info.ParameterType).Append(' ').Append(info.ParameterName).AppendLine(")")
            .AppendLine("        {")
            .AppendLine("            throw new global::System.NotImplementedException();")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}")
            .AppendLine();
    }
}
