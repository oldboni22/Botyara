using Botyara.Core.Context;
using Botyara.Core.Pipeline;

namespace Botyara.Core.Utilities;

public sealed class AccessorPipe(ContextAccessor accessor) : BotyaraPipe
{
    public override bool InVoid(PipeContext context)
    {
        accessor.Context = context;
        
        return true;
    }

    public override void FinallyVoid(PipeContext context)
    {
        accessor.Context = null;
    }
}
