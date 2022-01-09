using System;
using Microsoft.CodeAnalysis;

namespace EmbedResourceCSharp;

internal partial record class FileTemplate(FileTemplate? Child, INamedTypeSymbol Type, IMethodSymbol? Method, string? Path)
{
    private string AccessibilityText => Method!.DeclaredAccessibility switch
    {
        Accessibility.Private => "private",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.Public => "public",
        Accessibility.NotApplicable => throw new NotImplementedException(),
        _ => throw new ArgumentOutOfRangeException(),
    };
}
