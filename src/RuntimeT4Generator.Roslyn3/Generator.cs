﻿namespace RuntimeT4Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var token = context.CancellationToken;
        var options = new Options(context.AnalyzerConfigOptions.GlobalOptions);
        foreach (var file in context.AdditionalFiles)
        {
            var info = T4Info.Select(((file, context.AnalyzerConfigOptions), options), token);
            if (info is null)
            {
                continue;
            }

            var (hintName, code) = Utility.Generate(info, token);
            context.AddSource(hintName, code);
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
