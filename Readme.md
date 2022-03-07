Notes:

You can find the grammar files in GoogleApiDesign.ApiUtilities.Grammar.
You should configure your ANTLR config to put files in src/GoogleApiDesign.ApiUtilities. I put it in the Antlr4 directory for convenience.

# Setup project

To generate the required files you can use the command:

```bash
antlr4 -Dlanguage=CSharp ./src/GoogleApiDesign.ApiUtilities/Grammar/Filter.g4 -o ./src/GoogleApiDesign.ApiUtilities/Antlr4 -visitor
```

Also in the base of the project there is a script to do this for you:


```bash
./build_grammar.sh
```
