namespace RuntimeT4Generator;

public static partial class Utility
{
    public static void Generate(StringBuilder builder, int indent, Kind kind, string parameterName, string? indentParameterName, ReadOnlySpan<char> text, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        builder.Append(' ', indent);
        builder.Append("public void TransformAppend(");
        builder.Append(kind.GetType());
        builder.Append(' ');
        builder.Append(parameterName);
        if (indentParameterName is null)
        {
            builder.AppendLine(")");
            builder.Append(' ', indent);
            builder.AppendLine("{");
        }
        else
        {
            builder.Append(", int ");
            builder.Append(indentParameterName);
            builder.AppendLine(")");
        }

        builder.Append(' ', indent);
        builder.AppendLine("{");

        if (indentParameterName is null)
        {
            if (kind == Kind.Utf8)
            {
                builder.AppendGenerate(kind, text, parameterName, indent + 4, EmbedLiteralUtf8, token);
            }
            else
            {
                builder.AppendGenerate(kind, text, parameterName, indent + 4, EmbedLiteral, token);
            }
        }
        else
        {
            if (kind == Kind.Utf8)
            {
                builder.Append(' ', indent + 4).Append("global::System.Span<byte> ____").Append(indentParameterName).Append("____ = stackalloc byte[").Append(indentParameterName).Append("];").AppendLine();
                builder.Append(' ', indent + 4).Append("____").Append(indentParameterName).Append(@"____.Fill((byte)' ');").AppendLine();
                builder.AppendLine();

                builder.AppendGenerate(kind, text, parameterName, indentParameterName, indent + 4, EmbedLiteralUtf8, token);
            }
            else if (kind != Kind.StringBuilder)
            {
                builder.Append(' ', indent + 4).Append("global::System.Span<char> ____").Append(indentParameterName).Append("____ = stackalloc char[").Append(indentParameterName).Append("];").AppendLine();
                builder.Append(' ', indent + 4).Append("____").Append(indentParameterName).Append(@"____.Fill(' ');").AppendLine();
                builder.AppendLine();

                builder.AppendGenerate(kind, text, parameterName, indentParameterName, indent + 4, EmbedLiteral, token);
            }
            else
            {
                builder.AppendGenerate(kind, text, parameterName, indentParameterName, indent + 4, EmbedLiteral, token);
            }
        }

        builder.Append(' ', indent);
        builder.AppendLine("}");
    }

    public static void AppendPreprocess(StringBuilder builder, ref ReadOnlySpan<char> text, CancellationToken token)
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

        builder.AppendLine();
    }

    private static ReadOnlySpan<char> AppendCode(this StringBuilder builder, ReadOnlySpan<char> text, int indent, ref bool shouldIndent, CancellationToken token)
    {
        builder.Append(' ', indent);
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
                        shouldIndent = true;
                        text = text.Slice(2);
                    }
                    else if (text.Length >= 1 && text[0] == '\n')
                    {
                        shouldIndent = true;
                        text = text.Slice(1);
                    }

                    return text;
                default:
                    token.ThrowIfCancellationRequested();
                    if (i - 1 >= 0 && text[i - 1] == '\n')
                    {
                        builder.Append(' ', indent);
                    }

                    builder.Append(c);
                    break;
            }
        }

        return ReadOnlySpan<char>.Empty;
    }

    private static ReadOnlySpan<char> AppendValue(this StringBuilder builder, Kind kind, string parameterName, ReadOnlySpan<char> text, int indent, CancellationToken token)
    {
        builder.Append(' ', indent);
        builder.Append(parameterName);
        builder.Append(kind.AppendSomething());
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

        builder.AppendLine(");");
        return text.Slice(endIndex + 2);
    }

    private static void AppendGenerate(this StringBuilder builder, Kind kind, ReadOnlySpan<char> text, string parameterName, int indent, Embed embed, CancellationToken token)
    {
        while (!text.IsEmpty)
        {
            token.ThrowIfCancellationRequested();
            var codeIndex = text.IndexOf("<#".AsSpan(), StringComparison.Ordinal);
            if (codeIndex == 0)
            {
                if (text.Length > 2 && text[2] == '=')
                {
                    text = builder.AppendValue(kind, parameterName, text.Slice(3).TrimStart(), indent, token);
                }
                else
                {
                    bool _ = false;
                    text = builder.AppendCode(text.Slice(2), indent, ref _, token);
                }
            }
            else if (codeIndex == -1)
            {
                builder.Append(' ', indent);
                builder.Append(parameterName);
                builder.Append(kind.AppendLiteral());
                embed(builder, text);
                builder.AppendLine(");");
                break;
            }
            else
            {
                builder.Append(' ', indent);
                builder.Append(parameterName);
                builder.Append(kind.AppendLiteral());
                embed(builder, text.Slice(0, codeIndex));
                text = text.Slice(codeIndex);
                builder.AppendLine(");");
            }
        }
    }

    private static void AppendGenerate(this StringBuilder builder, Kind kind, ReadOnlySpan<char> text, string parameterName, string indentParameterName, int indent, Embed embed, CancellationToken token)
    {
        bool shouldIndent = true;
        void PreIndent()
        {
            if (!shouldIndent)
            {
                return;
            }

            shouldIndent = false;
            builder.Append(' ', indent);
            builder.Append("if (");
            builder.Append(indentParameterName);
            builder.Append(" > 0) ");
            builder.Append(parameterName);
            if (kind == Kind.StringBuilder)
            {
                builder.Append(".Append(' ', ");
                builder.Append(indentParameterName);
            }
            else
            {
                builder.Append(kind.AppendLiteral());
                builder.Append("____");
                builder.Append(indentParameterName);
                builder.Append("____");
            }

            builder.AppendLine(");");
        }

        ReadOnlySpan<char> SliceLine(ref ReadOnlySpan<char> text, int length)
        {
            var answer = text.Slice(0, length);
            text = text.Slice(length);
            if (text[0] == '\r' && text.Length > 1 && text[1] == '\n')
            {
                shouldIndent = true;
                text = text.Slice(2);
            }
            else if (text[0] == '\n')
            {
                shouldIndent = true;
                text = text.Slice(1);
            }

            return answer;
        }

        void ProcessCode(ref ReadOnlySpan<char> text)
        {
            if (text.Length > 2 && text[2] == '=')
            {
                PreIndent();
                text = builder.AppendValue(kind, parameterName, text.Slice(3).TrimStart(), indent, token);
            }
            else
            {
                bool _ = false;
                text = builder.AppendCode(text.Slice(2), indent, ref _, token);
            }
        }

        void AppendLine()
        {
            builder.Append(' ', indent);
            builder.Append(parameterName);
            builder.AppendLine(kind.AppendLine());
        }

        void AppendLiteral()
        {
            builder.Append(' ', indent);
            builder.Append(parameterName);
            builder.AppendLine(kind.AppendLiteral());
        }

        while (!text.IsEmpty)
        {
            token.ThrowIfCancellationRequested();
            var codeIndex = text.IndexOf("<#".AsSpan(), StringComparison.Ordinal);
            var crlfIndex = text.IndexOfAny('\r', '\n');
            if (codeIndex == 0)
            {
                ProcessCode(ref text);
                continue;
            }

            if (crlfIndex == 0)
            {
                SliceLine(ref text, 0);
                AppendLine();
                continue;
            }

            if (codeIndex == -1)
            {
                PreIndent();
                AppendLiteral();
                if (crlfIndex == -1)
                {
                    embed(builder, text);
                    builder.AppendLine(");");
                    break;
                }
                else
                {
                    var line = SliceLine(ref text, crlfIndex);
                    embed(builder, line);
                    builder.AppendLine(");");
                    AppendLine();
                }
            }
            else
            {
                PreIndent();
                AppendLiteral();
                if (crlfIndex > 0 && crlfIndex < codeIndex)
                {
                    var line = SliceLine(ref text, crlfIndex);
                    embed(builder, line);
                    builder.AppendLine(");");
                    AppendLine();
                }
                else
                {
                    embed(builder, text.Slice(0, codeIndex));
                    builder.AppendLine(");");
                    text = text.Slice(codeIndex);
                    ProcessCode(ref text);
                }
            }
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

    private static bool TryGetUsingNamespace(ReadOnlySpan<char> content, out ReadOnlySpan<char> namespaceSpan)
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
