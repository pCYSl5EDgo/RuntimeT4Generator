namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider.Select(Options.Select).WithComparer(EqualityComparer<Options>.Default);
        var isDesignTimeBuild = options.Select(static (options, token) => options.DesignTimeBuild == "true").WithComparer(EqualityComparer<bool>.Default);
        var files = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(options)
            .Select(Utility.SelectT4File)
            .Where(x =>
            {
                var (@namespace, @class, text) = x;
                return !string.IsNullOrWhiteSpace(@namespace) && !string.IsNullOrWhiteSpace(@class) && !string.IsNullOrWhiteSpace(text);
            })!
            .WithComparer(EqualityComparer<(string, string, string)>.Default);

        context.RegisterSourceOutput(files.Combine(isDesignTimeBuild), static (context, pair) =>
        {
            var ((@namespace, @class, text), isDesignTimeBuild) = pair;
            var (hintName, code) = Utility.Generate(@namespace, @class, text, isDesignTimeBuild, context.CancellationToken);
            context.AddSource(hintName, code);
        });
    }

}
