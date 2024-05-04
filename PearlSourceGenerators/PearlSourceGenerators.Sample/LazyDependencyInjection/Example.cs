namespace PearlSourceGenerators.Sample.LazyDependencyInjection;

[Generators.LazyDependencyInjection]
public partial class Example
{
    [Generators.LazyDependency] public ExampleDependencyService? _exampleService;

    public void Run()
    {
        ExampleService.DoStuff();
    }
}