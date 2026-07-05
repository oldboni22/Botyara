using Botyara.Core.Context;

namespace Botyara.Core.Pipeline;

public abstract class BotyaraPipe
{
    public virtual Task<bool> InTask(PipeContext context) => throw new InvalidOperationException();
    
    public virtual ValueTask<bool> InValueTask(PipeContext context) => throw new InvalidOperationException();

    public virtual bool InVoid(PipeContext context) => throw new InvalidOperationException();
    
    
    
    public virtual Task OutTask(PipeContext context) => throw new InvalidOperationException();
    
    public virtual ValueTask OutValueTask(PipeContext context) => throw new InvalidOperationException();
    
    public virtual void OutVoid(PipeContext context) => throw new InvalidOperationException();
    
    
    
    public virtual Task FinallyTask(PipeContext context) => throw new InvalidOperationException();
    
    public virtual ValueTask FinallyValueTask(PipeContext context) => throw new InvalidOperationException();
    
    public virtual void FinallyVoid(PipeContext context) => throw new InvalidOperationException();
}
