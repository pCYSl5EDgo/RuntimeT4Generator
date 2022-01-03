# RuntimeT4Generator

This is a replacement of [TextTemplatingFilePreprocessor](https://docs.microsoft.com/en-us/visualstudio/modeling/run-time-text-generation-with-t4-text-templates?view=vs-2022).

# Installation

Install via Nuget.

```
dotnet add package RuntimeT4Generator
```

# How to use

```xml:Example.csproj
<ItemGroup>
    <!-- Default Namespace is $(RootNamespace) -->
    <AdditionalFiles Include="A.tt" RuntimeT4Generator_Namespace="YourOwnNamespace" />
    <!-- Default Type Name is FileNameWithoutExtension -->
    <AdditionalFiles Include="B.tt" RuntimeT4Generator_TypeName="YourOwnClassName" />
    <!-- Default Parameter Name is builder -->
    <AdditionalFiles Include="D.tt" RuntimeT4Generator_ParameterName="buffer" />
    <!-- Default -->
    <AdditionalFiles Include="E.tt" RuntimeT4Generator="" />
    <!-- Multiple -->
    <AdditionalFiles Include="E.tt" RuntimeT4Generator="Utf8, Utf16, StringBuilder" />
</ItemGroup>
```

You can use `RuntimeT4Generator_Namespace`, `RuntimeT4Generator_TypeName` and `RuntimeT4Generator_ParameterName` at the same time.

This source generator support the 3 types.

|Type|RuntimeT4Generator Value|
|-|-|
|System.StringBuilder|"StringBuilder" or ""|
|System.StringBuilder|"StringBuilder" or ""|
|ref Cysharp.Text.Utf16ValueStringBuilder|"Utf16"|
|ref global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler|"DefaultInterpolatedStringHandler"|
