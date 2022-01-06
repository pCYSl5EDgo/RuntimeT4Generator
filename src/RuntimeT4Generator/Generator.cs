namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider.Select(Options.Select).WithComparer(EqualityComparer<Options>.Default);
        var files = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(options)
            .SelectMany(T4Info.Select)
            .WithComparer(T4Info.Comparer.Default);

        context.RegisterSourceOutput(files, static (context, info) =>
        {
            var (hintName, code) = Utility.Generate(info, context.CancellationToken);
            if (!string.IsNullOrEmpty(hintName))
            {
                context.AddSource(hintName, code);
            }
        });
    }

}
