namespace PearlSourceGenerators.Sample.AutoInjection;

[Generators.AutoInjection.AutoInjection]
public partial class Example
{
    [Generators.AutoInjection.AutoInject]
    public ExampleDependencyService SomeService { get; set; }
    
    
    [Generators.AutoInjection.AutoInject]
    public ExampleDependencyService SomeService2 { get; set; }
}