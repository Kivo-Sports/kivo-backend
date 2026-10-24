using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace kivoBackend.Tests.TestSupport;

/// <summary>
/// Hand-rolled in-memory <see cref="IUserStore{TUser}"/> (+ email/password/lockout/role
/// stores) backing a real <see cref="UserManager{TUser}"/> for tests — UsuarioService takes
/// a concrete UserManager&lt;IdentityUser&gt; as a dependency, so this is the boundary we
/// fake rather than mocking the service itself.
/// </summary>
public sealed class FakeUserStore :
    IUserStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserLockoutStore<IdentityUser>,
    IUserRoleStore<IdentityUser>
{
    private readonly Dictionary<string, IdentityUser> _usersById = new();
    private readonly Dictionary<string, HashSet<string>> _rolesByUserId = new();

    public bool ThrowOnCreate { get; set; }

    // ---- IUserStore ----
    public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken ct)
    {
        if (ThrowOnCreate) return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Falha simulada." }));
        user.Id = string.IsNullOrEmpty(user.Id) ? Guid.NewGuid().ToString() : user.Id;
        _usersById[user.Id] = user;
        _rolesByUserId[user.Id] = new HashSet<string>();
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken ct)
    {
        _usersById[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken ct)
    {
        _usersById.Remove(user.Id);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken ct)
        => Task.FromResult(_usersById.TryGetValue(userId, out var u) ? u : null);

    public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
        => Task.FromResult(_usersById.Values.FirstOrDefault(u =>
            string.Equals(u.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase)));

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.UserName);
    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
    public void Dispose() { }

    // ---- IUserEmailStore ----
    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken ct) { user.Email = email; return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.Email);
    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.EmailConfirmed);
    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken ct) { user.EmailConfirmed = confirmed; return Task.CompletedTask; }
    public Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => Task.FromResult(_usersById.Values.FirstOrDefault(u =>
            string.Equals(u.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase)));
    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken ct) { user.NormalizedEmail = normalizedEmail; return Task.CompletedTask; }

    // ---- IUserPasswordStore ----
    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken ct) { user.PasswordHash = passwordHash; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);
    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash != null);

    // ---- IUserLockoutStore ----
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.LockoutEnd);
    public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken ct) { user.LockoutEnd = lockoutEnd; return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(IdentityUser user, CancellationToken ct) { user.AccessFailedCount++; return Task.FromResult(user.AccessFailedCount); }
    public Task ResetAccessFailedCountAsync(IdentityUser user, CancellationToken ct) { user.AccessFailedCount = 0; return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.AccessFailedCount);
    public Task<bool> GetLockoutEnabledAsync(IdentityUser user, CancellationToken ct) => Task.FromResult(user.LockoutEnabled);
    public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled, CancellationToken ct) { user.LockoutEnabled = enabled; return Task.CompletedTask; }

    // ---- IUserRoleStore ----
    public Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken ct)
    {
        if (!_rolesByUserId.TryGetValue(user.Id, out var roles)) _rolesByUserId[user.Id] = roles = new HashSet<string>();
        roles.Add(roleName);
        return Task.CompletedTask;
    }
    public Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken ct)
    {
        if (_rolesByUserId.TryGetValue(user.Id, out var roles)) roles.Remove(roleName);
        return Task.CompletedTask;
    }
    public Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken ct)
        => Task.FromResult<IList<string>>(_rolesByUserId.TryGetValue(user.Id, out var roles) ? roles.ToList() : new List<string>());
    public Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken ct)
        => Task.FromResult(_rolesByUserId.TryGetValue(user.Id, out var roles) && roles.Contains(roleName));
    public Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken ct)
        => Task.FromResult<IList<IdentityUser>>(_usersById.Values.Where(u => _rolesByUserId.TryGetValue(u.Id, out var r) && r.Contains(roleName)).ToList());
}

/// <summary>Builds a real <see cref="UserManager{TUser}"/> wired to a <see cref="FakeUserStore"/> — no HTTP/DB involved.</summary>
public static class UserManagerTestFactory
{
    public static UserManager<IdentityUser> Create(FakeUserStore store)
    {
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser>();
        var userValidators = new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() };
        var passwordValidators = new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();

        var userManager = new UserManager<IdentityUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, NullLogger<UserManager<IdentityUser>>.Instance);

        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var tokenProvider = new DataProtectorTokenProvider<IdentityUser>(
            dataProtectionProvider, Options.Create(new DataProtectionTokenProviderOptions()),
            NullLogger<DataProtectorTokenProvider<IdentityUser>>.Instance);
        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, tokenProvider);

        return userManager;
    }
}
