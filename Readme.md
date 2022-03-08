# Google API Improvement Proposal (GAIP) Utilities for C#
This project is an attempt to create a simple to use library that implements Google's API Improvement Proposals. Currently, only [AIP-160](https://google.aip.dev/160) is being implemented, but features like sorting could follow. The project is still very fresh, so no stability is guaranteed outside of what is tested in the provided unit tests.

# Usage
At the moment this tool supports the conversion of Filter strings to Mongo `FilterDefinition<object>`. For example:
```csharp
var myFilter = "foo=bar OR foo!=baz";

var filterDefinition = FilterBuilder
                        .FromString(text)
                        .UseAdapter(new MongoFilterAdapter())
                        .Build<FilterDefinition<object>>;

IMongoCollection<SomeThing> myCollection = ... // However you want to instantiate your collection
 
myCollection.Find(filterDefinition.ToBsonDocument());
```
The resulting query would then look something like this:
```
{ $or : [ { foo : "bar" }, { foo : { $ne: "baz" } } ] }
```
# Development notes

To generate the required files you can use the command:

```bash
antlr4 -Dlanguage=CSharp ./src/GoogleApiDesign.ApiUtilities/Grammar/Filter.g4 -o ./src/GoogleApiDesign.ApiUtilities/Antlr4 -visitor
```

Also, in the base of the project there is a script to do this for you:

```bash
./build_grammar.sh
```

## Todo:
Note: You're all free to create issues to make any suggestions, and submit PRs. I will provide a document on contributing sometime in the future... 

Implementing [AIP-160](https://google.aip.dev/160), in no specific order of importance:
- [ ] Support for strongly typed `FilterDefinition` variant of the Mongo Driver (e.g. usage of `BsonId` and other attributes)
- [ ] Add build pipelines for build validation and distribution through nuget.org
- [ ] Add templates for pull requests
- [ ] Review "HAS" operator behaviour (more tests, make sure it confirms to Google guidelines)
- [ ] Support for functions
- [ ] Support for `IQueryable`, so we have at least LINQ support

## See also
[License.md](./License.md)

## Other Notes
You can find the grammar files in GoogleApiDesign.ApiUtilities.Grammar.
You should configure your ANTLR config to put files in src/GoogleApiDesign.ApiUtilities. I put it in the Antlr4 directory for convenience.
