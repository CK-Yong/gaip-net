using System;
using System.Collections.Generic;
using System.Linq;

namespace Gaip.Net.Core;

internal class WhitelistListener : FilterBaseListener, IHasErrors
{
    private readonly List<string> _whitelist;

    public WhitelistListener(List<string> whitelist)
    {
        _whitelist = whitelist;
    }

    public bool ErrorsFound { get; private set; }

    public override void ExitComparable(FilterParser.ComparableContext context)
    {
        var accessedMember = context.GetText();

        if (context.Parent is FilterParser.ArgContext)
        {
            base.ExitComparable(context);
            return;
        }
        
        if (!_whitelist.Any(x => string.Equals(x, accessedMember, StringComparison.InvariantCultureIgnoreCase)))
        {
            ErrorsFound = true;
        }
        
        base.ExitComparable(context);
    }
}