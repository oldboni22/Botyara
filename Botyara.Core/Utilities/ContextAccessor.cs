using Botyara.Core.Context;

namespace Botyara.Core.Utilities;

public sealed class ContextAccessor
{
    private sealed class ContextContainer
    {
        public PipeContext? Context;
    }
    
    private static readonly AsyncLocal<ContextContainer> Local =  new();

    public PipeContext? Context
    {
        get => Local.Value?.Context;
        
        internal set
        {
            Local.Value?.Context = null;

            Local.Value = value is null 
                ? null! 
                : new  ContextContainer { Context = value };
        }
    }
}
