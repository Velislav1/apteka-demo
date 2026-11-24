namespace Apteka.Identity.Contracts;

/// <summary>Names of users for screens of other modules (e.g. who made a sale).</summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Active users for the login screen (choose a user from a list, as in Apteka Expert 2).</summary>
    Task<IReadOnlyList<LoginUser>> ListActiveAsync(CancellationToken cancellationToken = default);
}

public sealed record LoginUser(string UserName, string DisplayName);
