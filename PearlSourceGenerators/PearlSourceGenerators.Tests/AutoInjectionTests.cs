using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace PearlSourceGenerators.Tests;

public class AutoInjectionTests
{
    [Fact]
    public void TestAutoInjection()
    {
        var inputClassText = """
                              // <auto-generated/>
                              namespace TestNamespace;
                              class AutoInjectedService {}
                              [Generators.AutoInjection.AutoInjection]
                              partial class AutoTest {
                                  [Generators.AutoInjection.AutoInject]
                                  private AutoInjectedService Injected { get; set; }
                                  [Generators.AutoInjection.AutoInject]
                                  private AutoInjectedService Injected2 { get; set; }
                              }
                              """;
        var expectedOutputText = """
                                 /// <auto-generated/>
                                 using System;
                                 namespace TestNamespace;
                                 partial class AutoTest
                                 {
                                     public AutoTest(TestNamespace.AutoInjectedService injected,TestNamespace.AutoInjectedService injected2)
                                     {
                                         this.Injected = injected;
                                         this.Injected2 = injected2;
                                     }
                                 }
                                 """;
        var generator = new AutoInjectionSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create(nameof(AutoInjectionTests),
            new[] { CSharpSyntaxTree.ParseText(inputClassText) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        var runResult = driver.RunGenerators(compilation).GetRunResult();
        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("AutoTest.g.cs"));
        Assert.Equal(expectedOutputText, generatedFileSyntax.GetText().ToString(),
                ignoreLineEndingDifferences: true);
    }
}