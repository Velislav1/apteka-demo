using Apteka.Infrastructure.Common.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Apteka.Web.Features.Identity.Session;

/// <summary>
/// A Blazor circuit is its own scope without an HTTP request; this keeps the scope's current user
/// in step with the circuit's authentication state (pattern from the ASP.NET Core docs).
/// </summary>
internal sealed class UserCircuitHandler(AuthenticationStateProvider authentication, CurrentUserAccessor accessor)
    : CircuitHandler, IDisposable
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        authentication.AuthenticationStateChanged += OnAuthenticationChanged;
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await authentication.GetAuthenticationStateAsync();
        accessor.User = ClaimsCurrentUser.From(state.User);
    }

    public void Dispose() => authentication.AuthenticationStateChanged -= OnAuthenticationChanged;

    private void OnAuthenticationChanged(Task<AuthenticationState> task) =>
        _ = task.ContinueWith(t => accessor.User = ClaimsCurrentUser.From(t.Result.User), TaskScheduler.Default);
}
