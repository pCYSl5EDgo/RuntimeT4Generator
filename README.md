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
    <!-- Default Class Name is FileNameWithoutExtension -->
    <AdditionalFiles Include="B.tt" RuntimeT4Generator_Class="YourOwnClassName" />
    <!-- Default String Builder Type is global::System.Text.StringBuilder -->
    <AdditionalFiles Include="C.tt" RuntimeT4Generator_ParameterType="ref Cysharp.Text.Utf8ValueStringBuilder" />
    <!-- Default Parameter Name is builder -->
    <AdditionalFiles Include="D.tt" RuntimeT4Generator_ParameterName="buffer" />
    <!-- Default -->
    <AdditionalFiles Include="E.tt" RuntimeT4Generator="" />
</ItemGroup>
```

You can use `RuntimeT4Generator_Namespace`, `RuntimeT4Generator_Class`, `RuntimeT4Generator_ParameterType` and `RuntimeT4Generator_ParameterName` at the same time.