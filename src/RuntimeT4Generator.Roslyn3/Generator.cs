namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var options = new Options(context.AnalyzerConfigOptions.GlobalOptions);
        var isDesignTimeBuild = options.DesignTimeBuild == "true";
        foreach (var file in context.AdditionalFiles)
        {
            var info = T4Info.Select(file, context.AnalyzerConfigOptions, options);
            if (info is null)
            {
                continue;
            }

            var (hintName, code) = Utility.Generate(info, isDesignTimeBuild, context.CancellationToken);
            context.AddSource(hintName, code);
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
