using System.Globalization;
using FishingLog.Mobile.Services.Authentication;
using FluentAssertions;
using Microsoft.Maui.Storage;
using NSubstitute;

namespace FishingLog.Mobile.Tests.Authentication;

/// <summary>
/// Tests secure persistence and recovery behavior for authentication sessions.
/// </summary>
public sealed class SecureTokenStoreTests
{
    private const string AccessTokenKey = "fishinglog.auth.access_token";
    private const string RefreshTokenKey = "fishinglog.auth.refresh_token";
    private const string ExpiresAtKey = "fishinglog.auth.access_token_expires_at_utc";
    private const string UserIdKey = "fishinglog.auth.current_user_id";
    private const string UserEmailKey = "fishinglog.auth.current_user_email";

    /// <summary>Verifies that a complete token set is saved with a UTC expiration value.</summary>
    [Fact]
    public async Task SaveTokensAsync_ValidTokens_SavesCompleteTokenSet()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        var sut = new SecureTokenStore(secureStorage);
        var expiresAt = new DateTimeOffset(2026, 8, 24, 14, 30, 0, TimeSpan.FromHours(2));

        await sut.SaveTokensAsync("access-token", "refresh-token", expiresAt);

        secureStorage.Received(1).Remove(AccessTokenKey);
        await secureStorage.Received(1).SetAsync(RefreshTokenKey, "refresh-token");
        await secureStorage.Received(1).SetAsync(
            ExpiresAtKey,
            expiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        await secureStorage.Received(1).SetAsync(AccessTokenKey, "access-token");
    }

    /// <summary>Verifies that blank access or refresh tokens are rejected.</summary>
    [Theory]
    [InlineData("", "refresh-token")]
    [InlineData("access-token", " ")]
    public async Task SaveTokensAsync_BlankToken_ThrowsArgumentException(string accessToken, string refreshToken)
    {
        var sut = new SecureTokenStore(Substitute.For<ISecureStorage>());

        var action = () => sut.SaveTokensAsync(accessToken, refreshToken, DateTimeOffset.UtcNow);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifies that a partially written session is cleared when secure storage fails.</summary>
    [Fact]
    public async Task SaveTokensAsync_StorageFails_ClearsSessionAndRethrows()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        secureStorage
            .SetAsync(ExpiresAtKey, Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Storage unavailable.")));
        var sut = new SecureTokenStore(secureStorage);

        var action = () => sut.SaveTokensAsync("access-token", "refresh-token", DateTimeOffset.UtcNow);

        await action.Should().ThrowAsync<InvalidOperationException>();
        AssertSessionKeysRemoved(secureStorage);
    }

    /// <summary>Verifies that valid stored authentication values are parsed and returned.</summary>
    [Fact]
    public async Task GetSessionAsync_ValidStoredValues_ReturnsSessionValues()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        var userId = Guid.NewGuid();
        var expiresAt = new DateTimeOffset(2026, 8, 24, 12, 30, 0, TimeSpan.Zero);
        secureStorage.GetAsync(AccessTokenKey).Returns("access-token");
        secureStorage.GetAsync(RefreshTokenKey).Returns("refresh-token");
        secureStorage.GetAsync(ExpiresAtKey).Returns(expiresAt.ToString("O", CultureInfo.InvariantCulture));
        secureStorage.GetAsync(UserIdKey).Returns(userId.ToString("D"));
        secureStorage.GetAsync(UserEmailKey).Returns("angler@example.com");
        var sut = new SecureTokenStore(secureStorage);

        (await sut.GetAccessTokenAsync()).Should().Be("access-token");
        (await sut.GetRefreshTokenAsync()).Should().Be("refresh-token");
        (await sut.GetAccessTokenExpiresAtUtcAsync()).Should().Be(expiresAt);
        (await sut.GetCurrentUserIdAsync()).Should().Be(userId);
        (await sut.GetCurrentUserEmailAsync()).Should().Be("angler@example.com");
    }

    /// <summary>Verifies that missing or blank secure values are treated as unavailable.</summary>
    [Fact]
    public async Task GetSessionAsync_MissingOrBlankValues_ReturnsNull()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        secureStorage.GetAsync(AccessTokenKey).Returns((string?)null);
        secureStorage.GetAsync(RefreshTokenKey).Returns(" ");
        secureStorage.GetAsync(ExpiresAtKey).Returns(string.Empty);
        secureStorage.GetAsync(UserIdKey).Returns((string?)null);
        secureStorage.GetAsync(UserEmailKey).Returns(" ");
        var sut = new SecureTokenStore(secureStorage);

        (await sut.GetAccessTokenAsync()).Should().BeNull();
        (await sut.GetRefreshTokenAsync()).Should().BeNull();
        (await sut.GetAccessTokenExpiresAtUtcAsync()).Should().BeNull();
        (await sut.GetCurrentUserIdAsync()).Should().BeNull();
        (await sut.GetCurrentUserEmailAsync()).Should().BeNull();
    }

    /// <summary>Verifies that malformed session metadata invalidates all session keys.</summary>
    [Theory]
    [InlineData(ExpiresAtKey, "not-a-date")]
    [InlineData(UserIdKey, "not-a-guid")]
    [InlineData(UserIdKey, "00000000-0000-0000-0000-000000000000")]
    public async Task GetMetadataAsync_MalformedValue_ClearsSession(string key, string value)
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        secureStorage.GetAsync(key).Returns(value);
        var sut = new SecureTokenStore(secureStorage);

        if (key == ExpiresAtKey)
            (await sut.GetAccessTokenExpiresAtUtcAsync()).Should().BeNull();
        else
            (await sut.GetCurrentUserIdAsync()).Should().BeNull();

        AssertSessionKeysRemoved(secureStorage);
    }

    /// <summary>Verifies that unreadable encrypted storage is reset and treated as signed out.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_StorageThrows_RemovesAllAndReturnsNull()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        secureStorage
            .GetAsync(AccessTokenKey)
            .Returns(Task.FromException<string?>(new InvalidOperationException("Cannot decrypt.")));
        var sut = new SecureTokenStore(secureStorage);

        var result = await sut.GetAccessTokenAsync();

        result.Should().BeNull();
        secureStorage.Received(1).RemoveAll();
    }

    /// <summary>Verifies that active-account metadata is validated, normalized, and saved.</summary>
    [Fact]
    public async Task SaveCurrentUserAsync_ValidUser_SavesNormalizedValues()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        var sut = new SecureTokenStore(secureStorage);
        var userId = Guid.NewGuid();

        await sut.SaveCurrentUserAsync(userId, "  angler@example.com  ");

        await secureStorage.Received(1).SetAsync(UserIdKey, userId.ToString("D"));
        await secureStorage.Received(1).SetAsync(UserEmailKey, "angler@example.com");
    }

    /// <summary>Verifies that invalid active-account values are rejected.</summary>
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "angler@example.com")]
    [InlineData("a6334722-5550-4b32-bc28-660552a0fb2f", " ")]
    public async Task SaveCurrentUserAsync_InvalidUser_ThrowsArgumentException(string userIdValue, string email)
    {
        var sut = new SecureTokenStore(Substitute.For<ISecureStorage>());

        var action = () => sut.SaveCurrentUserAsync(Guid.Parse(userIdValue), email);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifies that clearing a session removes every authentication key.</summary>
    [Fact]
    public void Clear_Always_RemovesEverySessionKey()
    {
        var secureStorage = Substitute.For<ISecureStorage>();
        var sut = new SecureTokenStore(secureStorage);

        sut.Clear();

        AssertSessionKeysRemoved(secureStorage);
    }

    private static void AssertSessionKeysRemoved(ISecureStorage secureStorage)
    {
        secureStorage.Received().Remove(AccessTokenKey);
        secureStorage.Received().Remove(RefreshTokenKey);
        secureStorage.Received().Remove(ExpiresAtKey);
        secureStorage.Received().Remove(UserIdKey);
        secureStorage.Received().Remove(UserEmailKey);
    }
}
