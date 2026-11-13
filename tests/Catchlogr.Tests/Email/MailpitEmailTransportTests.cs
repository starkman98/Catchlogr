using System.Net;
using System.Text.Json;
using Catchlogr.Infrastructure.Email;
using Catchlogr.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Resend;

namespace Catchlogr.Tests.Email;

/// <summary>Verifies captured messages preserve recipients, bodies, and provider failures.</summary>
public sealed class MailpitEmailTransportTests
{
    /// <summary>Account-action links and both body formats reach the local inbox unchanged.</summary>
    [Fact]
    public async Task SendAsync_Message_CapturesCompleteEmail()
    {
        JsonElement? payload = null;
        using var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            payload = JsonSerializer.Deserialize<JsonElement>(await request.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var transport = new MailpitEmailTransport(client, Options.Create(new EmailOptions
        {
            MailpitBaseUrl = new Uri("http://localhost:8025")
        }));
        var message = new EmailMessage
        {
            From = "Catchlogr <account@catchlogr.test>",
            Subject = "Confirm account",
            HtmlBody = "<a href='https://localhost:17056/confirm-email?code=synthetic'>Confirm</a>",
            TextBody = "https://localhost:17056/confirm-email?code=synthetic"
        };
        message.To.Add("angler@catchlogr.test");

        await transport.SendAsync(message, TestContext.Current.CancellationToken);

        handler.LastRequestUri.Should().Be("http://localhost:8025/api/v1/send");
        payload!.Value.GetProperty("from").GetProperty("email").GetString().Should().Be("account@catchlogr.test");
        payload.Value.GetProperty("to")[0].GetProperty("email").GetString().Should().Be("angler@catchlogr.test");
        payload.Value.GetProperty("html").GetString().Should().Be(message.HtmlBody);
        payload.Value.GetProperty("text").GetString().Should().Be(message.TextBody);
    }

    /// <summary>Inbox failure must fail delivery without exposing provider response bodies.</summary>
    [Fact]
    public async Task SendAsync_InboxUnavailable_RejectsDelivery()
    {
        using var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("sensitive-provider-response")
            }));
        using var client = new HttpClient(handler);
        var transport = new MailpitEmailTransport(client, Options.Create(new EmailOptions
        {
            MailpitBaseUrl = new Uri("http://localhost:8025")
        }));
        var message = new EmailMessage { From = "account@catchlogr.test" };
        message.To.Add("angler@catchlogr.test");

        var action = () => transport.SendAsync(message, TestContext.Current.CancellationToken);

        (await action.Should().ThrowAsync<HttpRequestException>()).Which.Message
            .Should().NotContain("sensitive-provider-response");
    }
}
