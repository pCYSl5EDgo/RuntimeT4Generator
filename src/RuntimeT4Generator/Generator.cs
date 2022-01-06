namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource("Attribute", Utility.Attribute));

        var options = context.AnalyzerConfigOptionsProvider.Select(Options.Select).WithComparer(EqualityComparer<Options>.Default);
        {
            var files = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(options)
            .SelectMany(T4Info.Select)
            .WithComparer(T4Info.Comparer.Default);
            context.RegisterSourceOutput(files, Generate);
        }

        {
            var attribute = context.CompilationProvider
                .Select(Utility.SelectT4Attribute)
                .WithComparer(SymbolEqualityComparer.Default);
            var projectDir = options.Select((options, token) =>
            {
                token.ThrowIfCancellationRequested();
                return options.ProjectDir;
            }).WithComparer(StringComparer.Ordinal);
            var attributeFiles = context.SyntaxProvider
                .CreateSyntaxProvider(Utility.Predicate, Utility.Transform)
                .Where(x => x is not null)
                .Combine(attribute.Combine(projectDir))
                .SelectMany(Utility.TransformToT4Info!)
                .WithComparer(T4Info.Comparer.Default);
            context.RegisterSourceOutput(attributeFiles, Generate);
        }
    }

    private static void Generate(SourceProductionContext context, T4Info info)
    {
        var (hintName, code) = Utility.Generate(info, context.CancellationToken);
        if (string.IsNullOrEmpty(hintName))
        {
            return;
        }

        context.AddSource(hintName, code);
    }
}
