using System.Text;

namespace RuntimeT4Generator;

public static class Utility
{
    private const string indent3 = "            ";

    public static (string HintName, string Code) Generate(T4Info info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        if (!GeneratePre(builder, info, out var text, token))
        {
            return default;
        }

        GenerateFull(builder, info, text, token);
        if (!string.IsNullOrWhiteSpace(info.RuntimeT4Generator_IndentParameterName))
        {
            builder.AppendLine();
            GenerateIndent(builder, info, text, info.RuntimeT4Generator_IndentParameterName!, token);
        }

        builder
            .AppendLine("    }")
            .AppendLine("}")
            .AppendLine();
        var code = builder.ToString();
        var hintName = builder.Clear().Append(info.Namespace).Append('.').Append(info.TypeName).Append('.').Append(info.RuntimeT4Generator).Append(".g.cs").ToString();
        return (hintName, code);
    }

    private static bool GeneratePre(StringBuilder builder, T4Info info, out ReadOnlySpan<char> span, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var text = info.Text.GetText(token)?.ToString();
        if (text is null)
        {
            span = default;
            return false;
        }

        span = text.AsSpan();
        builder
            .AppendPreprocess(ref span, token)
            .Append("namespace ").AppendLine(info.Namespace)
            .AppendLine("{")
            .Append("    ");
        builder
            .Append(info.Modifier)
            .Append(' ').AppendLine(info.TypeName)
            .AppendLine("    {");
        return true;
    }

    private static void GenerateFull(StringBuilder builder, T4Info info, ReadOnlySpan<char> span, CancellationToken token)
    {
        builder
            .Append("        public void TransformAppend(").Append(info.ParameterType).Append(' ').Append(info.ParameterName).AppendLine(")")
            .AppendLine("        {")
            .AppendGenerate(info, span, null, info.RuntimeT4Generator == "Utf8" ? EmbedLiteralUtf8 : EmbedLiteral, token)
            .AppendLine("        }");

        if (info.RuntimeT4Generator == "Utf8")
        {
            builder
                .AppendLine()
                .AppendLine("        private static void CopyTo(ref global::Cysharp.Text.Utf8ValueStringBuilder builder, global::System.ReadOnlySpan<byte> span)")
                .AppendLine("        {")
                .AppendLine(indent3 + "var destination = builder.GetSpan(span.Length);")
                .AppendLine(indent3 + "span.CopyTo(destination);")
                .AppendLine(indent3 + "builder.Advance(span.Length);")
                .AppendLine("        }");
        }
    }

    private static void GenerateIndent(StringBuilder builder, T4Info info, ReadOnlySpan<char> span, string indentParameterName, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        builder
            .Append("        public void TransformAppend(").Append(info.ParameterType).Append(' ').Append(info.ParameterName).Append(", uint ").Append(indentParameterName).AppendLine(")")
            .AppendLine("        {");

        if (info.RuntimeT4Generator == "Utf8")
        {
            builder.Append(indent3 + "global::System.Span<byte> ____").Append(indentParameterName).Append("____ = stackalloc byte[(int)").Append(indentParameterName).Append("];").AppendLine();
            builder.Append(indent3 + "____").Append(indentParameterName).Append(@"____.Fill((byte)' ');").AppendLine();
            builder.AppendLine();

            builder.AppendGenerate(info, span, indentParameterName, EmbedLiteralUtf8, token);
        }
        else
        {
            builder.Append(indent3 + "global::System.Span<char> ____").Append(indentParameterName).Append("____ = stackalloc char[(int)").Append(indentParameterName).Append("];").AppendLine();
            builder.Append(indent3 + "____").Append(indentParameterName).Append(@"____.Fill(' ');").AppendLine();
            builder.AppendLine();

            builder.AppendGenerate(info, span, indentParameterName, EmbedLiteral, token);
        }

        builder.AppendLine("        }");
    }

    private static StringBuilder AppendPreprocess(this StringBuilder builder, ref ReadOnlySpan<char> text, CancellationToken token)
    {
        ReadOnlySpan<char> specialStart = "<#@".AsSpan();
        ReadOnlySpan<char> end = "#>".AsSpan();

        while (!text.IsEmpty)
        {
            token.ThrowIfCancellationRequested();
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
            if (text.Length > 0 && text[0] == '\n')
            {
                text = text.Slice(1);
            }
            else if (text.Length > 1 && text[0] == '\r' && text[1] == '\n')
            {
                text = text.Slice(2);
            }

            continue;
        }

        return builder.AppendLine();
    }

    private static ReadOnlySpan<char> AppendCode(this StringBuilder builder, ReadOnlySpan<char> text, CancellationToken token)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '#' when i + 1 < text.Length && text[i + 1] == '>':
                    builder.AppendLine();
                    text = text.Slice(i + 2);
                    if (text.Length >= 2 && text[0] == '\r' && text[1] == '\n')
                    {
                        text = text.Slice(2);
                    }
                    else if (text.Length >= 1 && text[0] == '\n')
                    {
                        text = text.Slice(1);
                    }

                    return text;
                default:
                    token.ThrowIfCancellationRequested();
                    builder.Append(c);
                    break;
            }
        }

        return ReadOnlySpan<char>.Empty;
    }

    private static ReadOnlySpan<char> AppendValue(this StringBuilder builder, T4Info info, ReadOnlySpan<char> text, CancellationToken token)
    {
        builder.Append(indent3);
        builder.Append(info.MethodPrefix);
        var endIndex = text.IndexOf("#>".AsSpan(), StringComparison.Ordinal);
        if (endIndex <= 0)
        {
            return ReadOnlySpan<char>.Empty;
        }

        token.ThrowIfCancellationRequested();
        var valueSpan = text.Slice(0, endIndex).TrimEnd();
        foreach (var c in valueSpan)
        {
            builder.Append(c);
        }

        builder.AppendLine(info.MethodSuffix);
        return text.Slice(endIndex + 2);
    }

    private static StringBuilder AppendGenerate(this StringBuilder builder, T4Info info, ReadOnlySpan<char> text, string? indentParameterName, Embed embed, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        while (!text.IsEmpty)
        {
            switch (text[0])
            {
                case '<' when 1 < text.Length && text[1] == '#':
                    if (2 < text.Length && text[2] == '=')
                    {
                        text = builder.AppendValue(info, text.Slice(3).TrimStart(), token);
                    }
                    else
                    {
                        text = builder.AppendCode(text.Slice(2), token);
                    }
                    break;
                default:
                    var index = text.IndexOf("<#".AsSpan(), StringComparison.Ordinal);
                    if (indentParameterName is not null)
                    {
                        var anotherIndex = text.IndexOfAny('\r', '\n');
                        if (anotherIndex < 0)
                        {
                            if (index != 0)
                            {
                                Pre(builder, info, indentParameterName);
                                embed(builder, text.Slice(0, index));
                                builder.AppendLine(info.MethodLiteralSuffix);
                            }

                            text = text.Slice(index);
                            break;
                        }

                        if (anotherIndex < index)
                        {
                            if (text[anotherIndex] == '\r' && anotherIndex + 1 < text.Length && text[anotherIndex + 1] == '\n')
                            {
                                if (anotherIndex != 0)
                                {
                                    Pre(builder, info, indentParameterName);
                                    embed(builder, text.Slice(0, anotherIndex));
                                    builder.AppendLine(info.MethodLiteralSuffix);
                                }

                                builder.Append(indent3);
                                builder.AppendLine(info.MethodCrLf);
                                text = text.Slice(anotherIndex + 2);
                            }
                            else if (text[anotherIndex] == '\n')
                            {
                                if (anotherIndex != 0)
                                {
                                    Pre(builder, info, indentParameterName);
                                    embed(builder, text.Slice(0, anotherIndex));
                                    builder.AppendLine(info.MethodLiteralSuffix);
                                }

                                builder.Append(indent3);
                                builder.AppendLine(info.MethodCrLf);
                                text = text.Slice(anotherIndex + 1);
                            }

                            break;
                        }

                        PreIndent(builder, info, indentParameterName);
                    }

                    builder.Append(indent3);
                    builder.Append(info.MethodLiteralPrefix);

                    if (index < 0)
                    {
                        embed(builder, text);
                        text = ReadOnlySpan<char>.Empty;
                    }
                    else
                    {
                        embed(builder, text.Slice(0, index));
                        text = text.Slice(index);
                    }

                    builder.AppendLine(info.MethodLiteralSuffix);
                    break;
            }
        }

        return builder;

        static void PreIndent(StringBuilder builder, T4Info info, string indentParameterName)
        {
            builder.Append(indent3);
            builder.Append(info.MethodLiteralPrefix);
            builder.Append("____");
            builder.Append(indentParameterName);
            builder.Append("____");
            builder.AppendLine(info.MethodLiteralSuffix);
        }

        static void Pre(StringBuilder builder, T4Info info, string indentParameterName)
        {
            PreIndent(builder, info, indentParameterName);

            builder.Append(indent3);
            builder.Append(info.MethodLiteralPrefix);
        }
    }

    private static readonly string[] Bytes = new string[256];

    static Utility()
    {
        for (int i = 0; i < Bytes.Length; i++)
        {
            Bytes[i] = i.ToString("X2");
        }
    }

    private delegate void Embed(StringBuilder builder, ReadOnlySpan<char> text);

    private static unsafe void EmbedLiteralUtf8(StringBuilder builder, ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            builder.Append("global::System.ReadOnlySpan<byte>.Empty");
            return;
        }

        builder.Append("new global::System.ReadOnlySpan<byte>(new byte[] { ");
        var max = Encoding.UTF8.GetMaxByteCount(text.Length);
        var array = new byte[max];
        fixed (char* ptr = text)
        fixed (byte* dest = &array[0])
        {
            var actual = Encoding.UTF8.GetBytes(ptr, text.Length, dest, array.Length);
            if (actual != 0)
            {
                builder.Append("0x");
                builder.Append(Bytes[*dest]);
                if (actual != 1)
                {
                    for (byte* itr = dest + 1, end = dest + actual; itr != end; ++itr)
                    {
                        builder.Append(", 0x");
                        builder.Append(Bytes[*itr]);
                    }
                }
            }
        }

        builder.Append(" })");
    }

    private static void EmbedLiteral(StringBuilder builder, ReadOnlySpan<char> text)
    {
        builder.Append("@\"");
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '"':
                    builder.Append("\"\"");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');
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
}
