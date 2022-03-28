# Google API Improvement Proposal (GAIP) Utilities for C#
This project is an attempt to create a simple to use library that implements Google's API Improvement Proposals. Currently, only [AIP-160](https://google.aip.dev/160) is being implemented, but features like sorting could follow. The project is still very fresh, so no stability is guaranteed outside of what is tested in the provided unit tests.

# Packages
Currently, the following packages are available:

| Package        | Prerelease                                                                                                                              | Stable                                                                                                            |
|----------------|-----------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| Gaip.Net.Core  | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Core?includePreReleases=true)](https://www.nuget.org/packages/Gaip.Net.Core/)    | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Core)](https://www.nuget.org/packages/Gaip.Net.Core/)      |
| Gaip.Net.Mongo | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Mongo?includePreReleases=true)](https://www.nuget.org/packages/Gaip.Net.Mongo/)  | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Mongo)](https://www.nuget.org/packages/Gaip.Net.Mongo/)    |
| Gaip.Net.Linq  | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Linq?includePreReleases=true)](https://www.nuget.org/packages/Gaip.Net.Linq/)    | [![NuGet Badge](https://buildstats.info/nuget/Gaip.Net.Linq)](https://www.nuget.org/packages/Gaip.Net.Linq/)      |

# Usage
This library is mainly built around performing queries in Mongo. For example:
```csharp
var filterDefinition = FilterBuilder
    .FromString("foo=\"bar\" OR foo!=\"baz\"")
    .UseAdapter(new MongoFilterAdapter<object>())
    .Build();

IMongoCollection<SomeThing> myCollection = ... // However you want to instantiate your collection

myCollection.Find(filterDefinition);
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
    
myCollection.Find(filterDefinition);
```
Result:
```
{ $and : [ { _id : "abc" }, { baz : "def" } ] }
```
Check the [unit tests](./test) for more examples.

## LINQ
The library also provides an adapter for generating expressions that can be used in `.Where()` calls. This is useful for filtering collections using LINQ. For example:

```csharp
var list = new List<MyClass> 
{
    new () { Id = 1, foo = "bar" },
    new () { Id = 2, foo = "baz" },
    new () { Id = 3, foo = "fizzbuzz" }
};

var filter = FilterBuilder
    .FromString("foo=\"bar\" OR foo!=\"baz\"")
    .UseAdapter(new LinqFilterAdapter<MyClass>())
    .Build();
    
list.Where(filter).Select(x => x.Id).ToList(); // [1, 3]
```

# Extending functionality
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

## See also
[License.md](./License.md)

## Other Notes
You can find the grammar files in `src/Gaip.Net.Core/Grammar`.
You should configure your ANTLR config to put files in `src/Gaip.Net.Core/...`. I put it in a `src/Gaip.Net.Core/Antlr4` directory for convenience.
