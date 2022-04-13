using System;
using System.Collections.Generic;
using System.Linq;

namespace Gaip.Net.Core;

internal class BlacklistListener : FilterBaseListener, IHasErrors
{
    private readonly List<string> _blacklist;

    public BlacklistListener(List<string> blacklist)
    {
        _blacklist = blacklist;
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
        
        if (_blacklist.Any(x => string.Equals(x, accessedMember, StringComparison.InvariantCultureIgnoreCase)))
        {
            ErrorsFound = true;
        }
        
        base.ExitComparable(context);
    }
}