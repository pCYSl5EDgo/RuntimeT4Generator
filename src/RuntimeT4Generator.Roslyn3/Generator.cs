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
            var(@namespace, @class, text) = Utility.SelectT4File(((file, context.AnalyzerConfigOptions), options), context.CancellationToken);
            if (!string.IsNullOrWhiteSpace(@namespace) && !string.IsNullOrWhiteSpace(@class) && !string.IsNullOrWhiteSpace(text))
            {
                var (hintName, code) = Utility.Generate(@namespace!, @class!, text!, isDesignTimeBuild, context.CancellationToken);
                context.AddSource(hintName, code);
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
