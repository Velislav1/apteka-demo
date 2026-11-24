namespace Apteka.Identity.Contracts;

/// <summary>Names of users for screens of other modules (e.g. who made a sale).</summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
}
