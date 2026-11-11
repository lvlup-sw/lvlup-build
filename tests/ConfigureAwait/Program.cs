var previous = SynchronizationContext.Current;
var context = new CountingContext();
SynchronizationContext.SetSynchronizationContext(context);
var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
var work = Observe(gate.Task);
SynchronizationContext.SetSynchronizationContext(previous);
gate.SetResult(7);
if (work.GetAwaiter().GetResult() != 22 || context.Posts != (args.Contains("opt-out") ? 2 : 1))
    throw new Exception($"Unexpected result or post count {context.Posts}.");

static async Task<int> Observe(Task<int> gate)
{
    var result = await gate;
    await Task.CompletedTask;
    await new ValueTask(Task.CompletedTask);
    result += await new ValueTask<int>(8);
    await foreach (var value in Values())
        result += value;
    await using var resource = new Resource();
    // A separate incomplete await must continue to honor the explicit override.
    var context = CountingContext.Instance;
    SynchronizationContext.SetSynchronizationContext(context);
    try
    {
        result += await Task.Run(async () => { await Task.Delay(30).ConfigureAwait(false); return 5; }).ConfigureAwait(true);
    }
    finally
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }
    return result;
}

static async IAsyncEnumerable<int> Values()
{
    await Task.CompletedTask;
    yield return 2;
}

sealed class Resource : IAsyncDisposable
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class CountingContext : SynchronizationContext
{
    public static CountingContext Instance { get; private set; } = null!;
    private int posts;
    public int Posts => posts;
    public CountingContext() => Instance = this;
    public override void Post(SendOrPostCallback callback, object? state)
    {
        Interlocked.Increment(ref posts);
        ThreadPool.QueueUserWorkItem(_ => callback(state));
    }
}
