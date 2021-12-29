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
            .Select(T4Info.Select)
            .Where(x => x is not null)!
            .WithComparer(EqualityComparer<T4Info>.Default);

        context.RegisterSourceOutput(files, static (context, info) =>
        {
            var (hintName, code) = Utility.Generate(info, context.CancellationToken);
            context.AddSource(hintName, code);
        });
    }

}
