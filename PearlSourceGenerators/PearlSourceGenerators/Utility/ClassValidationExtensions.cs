using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PearlSourceGenerators.Utility;

/// <summary>
/// Utilities for validating that a class should be processed.
/// </summary>
public static class GeneratorValidationExtensions
{
    /// <summary>
    /// Check if a given member (ie. class, field, method, etc) has at least one instance of an attribute present.
    /// </summary>
    /// <param name="member">The member to check</param>
    /// <param name="context">The syntax context of the generator</param>
    /// <param name="attributeType">The fully qualified type name of the attribute</param>
    /// <returns>true if the attribute is present, false if it is not</returns>
    public static bool HasAttribute(this MemberDeclarationSyntax member, GeneratorSyntaxContext context,
        string attributeType)
    {
        var attrs = member.AttributeLists
            .SelectMany(al => al.Attributes);
        
        foreach (var attr in attrs)
        {
            if (context.SemanticModel.GetSymbolInfo(attr).Symbol is not IMethodSymbol attrSym)
                continue;

            if (attrSym.ContainingType.ToDisplayString() != attributeType)
                continue;
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if any class members have at least one instance of an attribute present
    /// </summary>
    /// <param name="cls">Class to check</param>
    /// <param name="context">The syntax context of the source generator</param>
    /// <param name="attributeType">The fully-qualified name of the attribute</param>
    /// <returns>true if the attribute is present, false if it is not</returns>
    public static bool AnyClassMembersHave(this ClassDeclarationSyntax cls, GeneratorSyntaxContext context,
        string attributeType)
    {
        return cls.Members
            .Any(p => p.HasAttribute(context, attributeType));   
    }
    
    /// <summary>
    /// Check if any class members of the given type have at least one instance of an attribute present
    /// </summary>
    /// <typeparam name="T">The parameter type to check</typeparam>
    /// <param name="cls">Class to check</param>
    /// <param name="context">The syntax context of the source generator</param>
    /// <param name="attributeType">The fully-qualified name of the attribute</param>
    /// <returns>true if the attribute is present, false if it is not</returns>
    public static bool AnyClassMembersHave<T>(this ClassDeclarationSyntax cls, GeneratorSyntaxContext context,
        string attributeType) where T : MemberDeclarationSyntax
    {
        return cls.Members
            .OfType<T>()
            .Any(p => p.HasAttribute(context, attributeType));   
    }
}