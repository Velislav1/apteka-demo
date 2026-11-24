namespace Apteka.Web.Features.Shared;

/// <summary>
/// Runs a page's loads one after another. A Blazor circuit shares one database connection, so two loads started
/// by quick changes of a filter (e.g. a date) must not run at the same time.
/// </summary>
public sealed class OneAtATime : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task RunAsync(Func<Task> work)
    {
        await _gate.WaitAsync();
        try
        {
            await work();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
