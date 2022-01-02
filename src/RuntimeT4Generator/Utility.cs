using System.Text;

namespace RuntimeT4Generator;

public static class Utility
{
    public static (string HintName, string Code) Generate(T4Info info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        if (!GeneratePre(builder, info, out var text, token))
        {
            return default;
        }

        GenerateFull(builder, info, text, token);
        if (!string.IsNullOrWhiteSpace(info.RuntimeT4Generator_SupportsIndent))
        {
            builder.AppendLine();
            GenerateIndent(builder, info, text, info.RuntimeT4Generator_SupportsIndent!, token);
        }

        builder
            .AppendLine("    }")
            .AppendLine("}")
            .AppendLine();
        var code = builder.ToString();
        var hintName = builder.Clear().Append(info.Namespace).Append('.').Append(info.Class).Append('.').Append(info.RuntimeT4Generator).Append(".g.cs").ToString();
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
            .Append("    public partial class ").AppendLine(info.Class)
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
                .AppendLine("            var destination = builder.GetSpan(span.Length);")
                .AppendLine("            span.CopyTo(destination);")
                .AppendLine("            builder.Advance(span.Length);")
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
            builder.Append("            global::System.Span<byte> ____").Append(indentParameterName).Append("____ = stackalloc byte[(int)").Append(indentParameterName).Append("];").AppendLine();
            builder.Append("            ____").Append(indentParameterName).Append(@"____.Fill((byte)' ');").AppendLine();
            builder.AppendLine();

            builder.AppendGenerate(info, span, indentParameterName, EmbedLiteralUtf8CrLf, token);
        }
        else
        {
            builder.Append("            global::System.Span<char> ____").Append(indentParameterName).Append("____ = stackalloc char[(int)").Append(indentParameterName).Append("];").AppendLine();
            builder.Append("            ____").Append(indentParameterName).Append(@"____.Fill(' ');").AppendLine();
            builder.AppendLine();

            builder.AppendGenerate(info, span, indentParameterName, EmbedLiteralCrLf, token);
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

    private static StringBuilder AppendGenerate(this StringBuilder builder, T4Info info, ReadOnlySpan<char> text, string? indentParameterName, Embed embed, CancellationToken token)
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

                builder.Append(indent3).Append(info.MethodPrefix);
                foreach (var c in text.Slice(0, endIndex).Trim())
                {
                    builder.Append(c);
                }

                builder.AppendLine(info.MethodSuffix);
                text = text.Slice(endIndex + end.Length);
                goto STEP;
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
                        builder.AppendLine();
                        goto HEAD;
                    }

                    builder.Append(c);
                }

                break;
            }

        EMBED_LITERAL:
            if (indentParameterName is not null)
            {
                builder.Append(indent3).Append(info.MethodLiteralPrefix).Append("____").Append(indentParameterName).Append("____").AppendLine(info.MethodLiteralSuffix);
            }

            builder.Append(indent3).Append(info.MethodLiteralPrefix);
            if (embed(builder, ref text))
            {
                builder.AppendLine(info.MethodLiteralSuffix);
                if (text[0] == '\n')
                {
                    text = text.Slice(1);
                    builder.Append(indent3).AppendLine(info.MethodCrLf);
                    goto EMBED_LITERAL;
                }
                else if (text[0] == '\r' && text.Length >= 2 && text[1] == '\n')
                {
                    text = text.Slice(2);
                    builder.Append(indent3).AppendLine(info.MethodCrLf);
                    goto EMBED_LITERAL;
                }

                goto HEAD;
            }

            builder.AppendLine(info.MethodLiteralSuffix);
            break;
        }

    RETURN:
        return builder;
    }

    private static readonly string[] Bytes = new string[256];

    static Utility()
    {
        for (int i = 0; i < Bytes.Length; i++)
        {
            Bytes[i] = i.ToString("X2");
        }
    }

    private delegate bool Embed(StringBuilder builder, ref ReadOnlySpan<char> text);

    private static bool EmbedLiteralUtf8(StringBuilder builder, ref ReadOnlySpan<char> text)
    {
        builder.Append("new global::System.ReadOnlySpan<byte>(new byte[] { ");
        var array = Array.Empty<byte>();
        var sliceLength = text.Length;
        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] == '#' && text[i - 1] == '<')
            {
                sliceLength = i - 1;
                break;
            }
        }

        if (sliceLength > 0)
        {
            unsafe
            {
                fixed (char* ptr = text.Slice(0, sliceLength))
                {
                    var max = Encoding.UTF8.GetMaxByteCount(sliceLength);
                    if (max > array.Length)
                    {
                        array = new byte[max];
                    }

                    fixed (byte* dest = &array[0])
                    {
                        var actual = Encoding.UTF8.GetBytes(ptr, sliceLength, dest, array.Length);
                        if (actual != 0)
                        {
                            builder.Append("0x").Append(Bytes[*dest]);
                            if (actual != 1)
                            {
                                for (byte* itr = dest + 1, end = dest + actual; itr != end; ++itr)
                                {
                                    builder.Append(", 0x").Append(Bytes[*itr]);
                                }
                            }
                        }
                    }
                }
            }
        }

        builder.Append(" })");
        var answer = text.Length != sliceLength;
        if (answer)
        {
            text = text.Slice(sliceLength);
        }

        return answer;
    }

    private static bool EmbedLiteral(StringBuilder builder, ref ReadOnlySpan<char> text)
    {
        builder.Append("@\"");
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
                        builder.Append('"');
                        text = text.Slice(i);
                        return true;
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

        builder.Append('"');
        return false;
    }

    private static bool EmbedLiteralUtf8CrLf(StringBuilder builder, ref ReadOnlySpan<char> text)
    {
        builder.Append("new global::System.ReadOnlySpan<byte>(new byte[] { ");
        var array = Array.Empty<byte>();
        var sliceLength = text.Length;
        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] == '#' && text[i - 1] == '<')
            {
                sliceLength = i - 1;
                break;
            }
            else if (text[i] == '\n')
            {
                if (text[i - 1] == '\r')
                {
                    sliceLength = i - 1;
                    break;
                }
                else
                {
                    sliceLength = i;
                    break;
                }
            }
        }

        if (sliceLength > 0)
        {
            unsafe
            {
                fixed (char* ptr = text.Slice(0, sliceLength))
                {
                    var max = Encoding.UTF8.GetMaxByteCount(sliceLength);
                    if (max > array.Length)
                    {
                        array = new byte[max];
                    }

                    fixed (byte* dest = &array[0])
                    {
                        var actual = Encoding.UTF8.GetBytes(ptr, sliceLength, dest, array.Length);
                        if (actual != 0)
                        {
                            builder.Append("0x").Append(Bytes[*dest]);
                            if (actual != 1)
                            {
                                for (byte* itr = dest + 1, end = dest + actual; itr != end; ++itr)
                                {
                                    builder.Append(", 0x").Append(Bytes[*itr]);
                                }
                            }
                        }
                    }
                }
            }
        }

        builder.Append(" })");
        var answer = text.Length != sliceLength;
        if (answer)
        {
            text = text.Slice(sliceLength);
        }

        return answer;
    }

    private static bool EmbedLiteralCrLf(StringBuilder builder, ref ReadOnlySpan<char> text)
    {
        builder.Append("@\"");
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '\r':
                case '\n':
                    builder.Append('"');
                    text = text.Slice(i);
                    return true;
                case '<':
                    if (i + 1 < text.Length && text[i + 1] != '#')
                    {
                        builder.Append('<');
                    }
                    else
                    {
                        builder.Append('"');
                        text = text.Slice(i);
                        return true;
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

        builder.Append('"');
        return false;
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
