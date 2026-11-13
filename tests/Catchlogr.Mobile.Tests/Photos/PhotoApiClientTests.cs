using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Services.Photos;
using Catchlogr.Mobile.Tests.Authentication;
using FluentAssertions;
using NSubstitute;
using System.Net;

namespace Catchlogr.Mobile.Tests.Photos;

/// <summary>Checks private photo references download into the active account.</summary>
public sealed class PhotoApiClientTests
{
    /// <summary>Both API-relative references and uploaded absolute URLs identify the same private photo.</summary>
    [Theory]
    [InlineData("/api/photos/10000000-0000-0000-0000-000000000002")]
    [InlineData("http://10.0.2.2:15001/api/photos/10000000-0000-0000-0000-000000000002")]
    public async Task DownloadAsync_ProtectedReference_CachesExactBytes(string reference)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Catchlogr.PhotoTests", Guid.NewGuid().ToString("N"));
        var storage = Substitute.For<IAccountStorageContext>();
        storage.ActiveAccountDirectory.Returns(directory);
        byte[] bytes = [0xff, 0xd8, 0xff, 0x01];
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/photos/10000000-0000-0000-0000-000000000002");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            });
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new PhotoApiClient(client, storage);
        try
        {
            var path = await sut.DownloadAsync(reference, TestContext.Current.CancellationToken);
            path.Should().StartWith(Path.Combine(directory, "photos", "server"));
            (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).Should().Equal(bytes);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
