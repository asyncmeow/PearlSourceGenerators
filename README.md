# Pearl's Source Generators
A set of source generators, mostly written for my own convenience. Based on the template from JetBrains for a Roslyn
source generator.


## Content
### PearlSourceGenerators
A .NET Standard project with implementations of sample source generators.
**You must build this project to see the result (generated code) in the IDE.**

- [LazyDependencyInjectionSourceGenerator.cs](PearlSourceGenerators/PearlSourceGenerators/LazyDependencyInjectionSourceGenerator.cs): 
  A source generator that helps in creating a "lazy-loaded" DI wrapper class, where dependencies are pulled when they
  are needed, instead of on class creation.


### PearlSourceGenerators.Sample
A project that references source generators. Note the parameters of `ProjectReference` in 
[PearlSourceGenerators.Sample.csproj](PearlSourceGenerators/PearlSourceGenerators.Sample/PearlSourceGenerators.Sample.csproj),
they make sure that the project is referenced as a set of source generators. 

### PearlSourceGenerators.Tests
Unit tests for source generators. The easiest way to develop language-related features is to start with unit tests, even
if the unit test always passes while you are still developing. Note that before a new source generator is merged into
`main`, it must have at least one unit test that can fail if the source generator is broken.

## How To?****
### How to debug?
- Use the [launchSettings.json](Properties/launchSettings.json) profile.
- Debug tests.