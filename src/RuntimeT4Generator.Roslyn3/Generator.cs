namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var token = context.CancellationToken;
        var options = new Options(context.AnalyzerConfigOptions.GlobalOptions);
        foreach (var file in context.AdditionalFiles)
        {
            foreach (var info in T4Info.Select(((file, context.AnalyzerConfigOptions), options), token))
            {
                var (hintName, code) = Utility.Generate(info, token);
                context.AddSource(hintName, code);
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
