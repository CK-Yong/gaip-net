# Google API Improvement Proposal (GAIP) Utilities for C#
This project is an attempt to create a simple to use library that implements Google's API Improvement Proposals. Currently, only [AIP-160](https://google.aip.dev/160) is being implemented, but features like sorting could follow. The project is still very fresh, so no stability is guaranteed outside of what is tested in the provided unit tests.

# Packages
Currently, the following packages are available:

| Package        | Prerelease                                                                                                                             |                                                                                                             
|----------------|----------------------------------------------------------------------------------------------------------------------------------------|
| Gaip.Net.Core  | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Core?includePreReleases=true)](https://www.nuget.org/packages/Gaip.Net.Core/)   |  
| Gaip.Net.Mongo | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Mongo?includePreReleases=true)](https://www.nuget.org/packages/Gaip.Net.Mongo/) |

# Usage
At the moment this tool supports the conversion of Filter strings to Mongo `FilterDefinition<T>`. For example:
```csharp
var filterDefinition = FilterBuilder
    .FromString("foo=\"bar\" OR foo!=\"baz\"")
    .UseAdapter(new MongoFilterAdapter<object>())
    .Build();

IMongoCollection<SomeThing> myCollection = ... // However you want to instantiate your collection

myCollection.Find(filterDefinition.ToBsonDocument());
```
The resulting query would then look something like this:
```
{ $or : [ { foo : "bar" }, { foo : { $ne: "baz" } } ] }
```

Or if you are using alternative names:
```csharp
public class MyClass{
    [BsonId]
    public string Foo { get; set; }
    
    [BsonElement("baz")]
    public string Bar { get; set; }
}

var filterDefinition = FilterBuilder
    .FromString("Foo=\"abc\" AND Bar=\"def\"")
    .UseAdapter(new MongoFilterAdapter<MyClass>())
    .Build();
    
myCollection.Find(filterDefinition.ToBsonDocument());
```
Result:
```
{ $and : [ { _id : "abc" }, { baz : "def" } ] }
```
Check the unit tests for more examples.

## Extending functionality
At the moment, only queries that are compatible with the Mongo C# driver are supported. You are of course free to extend this library to support other databases. The easiest way to do this is to implement the `IFilterAdapter` interface. See also the [MongoFilterAdapter](./src/Gaip.Net.Mongo/MongoFilterAdapter.cs) class.

# Development notes
This project depends on Antlr4, and grammar files are specified in `src/Gaip.Net.Core/Grammar`. The easiest way to work with this is to use an Antlr4 plugin for your IDE. For example, the [Rider plugin](https://plugins.jetbrains.com/plugin/7358-antlr-v4). You can configure the plugin to export the generated Antlr classes to `src/Gaip.Net.Core/Antlr4` so they will be ignored by Git.

You can also generate the required files with the following command:

```bash
antlr4 -Dlanguage=CSharp ./src/Gaip.Net.Core/Grammar/Filter.g4 -o ./src/Gaip.Net.Core/Antlr4 -visitor
```

Also, in the base of the project there is a script to do this for you:

```bash
./build_grammar.sh
```

## Todo:
Note: You're all free to create issues to make any suggestions, and submit PRs. I will provide a document on contributing sometime in the future... 

Implementing [AIP-160](https://google.aip.dev/160), in no specific order of importance:
- [x] Support for strongly typed `FilterDefinition` variant of the Mongo Driver (e.g. usage of `BsonId` and other attributes)
- [x] Add build pipelines for build validation and distribution through nuget.org
- [ ] Add templates for pull requests
- [ ] Review "HAS" operator behaviour (more tests, make sure it confirms to Google guidelines)
- [ ] Support for functions
- [ ] Support for `IQueryable`, so we have at least LINQ support
- [ ] Support for [Ordering](https://google.aip.dev/132#ordering)

## See also
[License.md](./License.md)

## Other Notes
You can find the grammar files in GoogleApiDesign.ApiUtilities.Grammar.
You should configure your ANTLR config to put files in src/GoogleApiDesign.ApiUtilities. I put it in the Antlr4 directory for convenience.
