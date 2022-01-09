namespace RuntimeT4Generator;

[Flags]
public enum Kind
{
    StringBuilder = 1,
    Utf8 = 2,
    Utf16 = 4,
    DefaultInterpolatedStringHandler = 8,
}

public static class KindExtensions
{
    public static string GetTypeName(this Kind kind) => kind switch
    {
        Kind.StringBuilder => "global::System.Text.StringBuilder",
        Kind.Utf8 => "ref global::Cysharp.Text.Utf8ValueStringBuilder",
        Kind.Utf16 => "ref global::Cysharp.Text.Utf16ValueStringBuilder",
        Kind.DefaultInterpolatedStringHandler => "ref global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static string AppendLine(this Kind kind) => kind switch
    {
        Kind.DefaultInterpolatedStringHandler => ".AppendLiteral(global::System.Environment.NewLine);",
        _ => ".AppendLine();",
    };

    public static string AppendLiteral(this Kind kind) => kind switch
    {
        Kind.Utf8 => ".AppendLiteral(",
        Kind.DefaultInterpolatedStringHandler => ".AppendFormatted(",
        _ => ".Append(",
    };

    public static string AppendSomething(this Kind kind) => kind switch
    {
        Kind.DefaultInterpolatedStringHandler => ".AppendFormatted(",
        _ => ".Append(",
    };
}
