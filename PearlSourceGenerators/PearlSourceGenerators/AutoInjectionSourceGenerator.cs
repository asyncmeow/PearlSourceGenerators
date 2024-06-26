using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PearlSourceGenerators;

[Generator]
public class AutoInjectionSourceGenerator : IIncrementalGenerator
{
    private const string Namespace = "Generators.AutoInjection";
    private const string ClassAttribute = "AutoInjectionAttribute";
    private const string PropertyAttribute = "AutoInjectAttribute";
    
    private const string Attributes = $$"""
                                        // <auto-generated/>
                                        namespace {{Namespace}}
                                        {
                                            [System.AttributeUsage(System.AttributeTargets.Class)]
                                            public class {{ClassAttribute}} : System.Attribute
                                            {
                                            }
                                        
                                            [System.AttributeUsage(System.AttributeTargets.Property)]
                                            public class {{PropertyAttribute}} : System.Attribute
                                            {
                                            }
                                        }
                                        """;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => 
            ctx.AddSource("Attributes.g.cs",
                SourceText.From(Attributes, Encoding.UTF8)));
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.attributesFound)
            .Select((t, _) => t.Item1);
        
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right));
    }

    private static (ClassDeclarationSyntax, bool attributesFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var hasClassAttr = false;
        var hasPropAttr = false;
        
        // check if the class has the attribute we expect
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue; // ignore symbols we can't get
            var attrName = attributeSymbol.ContainingType.ToDisplayString();
            
            if (attrName != $"{Namespace}.{ClassAttribute}") 
                continue;
            
            hasClassAttr = true;
            break;
        }
        
        // check if any fields have the attribute we expect
        var fields = classDeclarationSyntax.Members
            .Where(m => m is PropertyDeclarationSyntax);
        foreach (var field in fields)
        foreach (var attributeListSyntax in field.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue;
            var attrName = attributeSymbol.ContainingType.ToDisplayString();

            if (attrName != $"{Namespace}.{PropertyAttribute}")
                continue;
            
            hasPropAttr = true;
            break;
        }
        
        return (classDeclarationSyntax, hasClassAttr && hasPropAttr);
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        foreach (var classDecl in classDeclarations)
        {
            // the semantic model of a class lets us retrieve its metadata easier
            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            
            // symbols let us get compile-time info like types
            if (ModelExtensions.GetDeclaredSymbol(semanticModel, classDecl) is not INamedTypeSymbol classSymbol)
                continue;
            
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "PSG001", 
                    "Class must be partial", 
                    "Class must be partial", 
                    "PearlSourceGenerators",
                    DiagnosticSeverity.Warning, true),
                    classDecl.GetLocation()));
            }
            var properties = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p =>
                {
                    foreach (var attr in p.GetAttributes())
                    {
                        if (attr.AttributeClass == null) continue;
                        var currentAttrClass = $"{attr.AttributeClass.ContainingNamespace}.{attr.AttributeClass.Name}";
                        if (currentAttrClass != $"{Namespace}.{PropertyAttribute}")
                            continue;
                        return true;
                    }

                    return false;
                })
                .ToList();
            var source = GenerateClassSource(className, namespaceName, properties);
            context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string ToCamelCase(string name)
    {
        return $"{name[0].ToString().ToLower()}{name.Substring(1)}";
    }
    
    private static string GenerateClassSource(string className, string namespaceName,
        List<IPropertySymbol> properties)
    {
        var constructorParts = properties.Select(property =>
                $"{property.Type.ContainingNamespace.ToDisplayString()}.{property.Type.Name} {ToCamelCase(property.Name)}")
            .ToList();
        var assignments = properties.Select(property => $"this.{property.Name} = {ToCamelCase(property.Name)};");
        return $$"""
                 /// <auto-generated/>
                 using System;
                 namespace {{namespaceName}};
                 partial class {{className}}
                 {
                     public {{className}}({{string.Join(",", constructorParts)}})
                     {
                 {{string.Join("\r\n", assignments.Select(s => $"        {s}"))}}
                     }
                 }
                 """;
    }
}