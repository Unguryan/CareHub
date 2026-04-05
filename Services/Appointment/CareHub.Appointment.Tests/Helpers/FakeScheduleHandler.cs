using System.Net;
using System.Text;

namespace CareHub.Appointment.Tests.Helpers;

public sealed class FakeScheduleHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.EndsWith("/api/slots/validate", StringComparison.OrdinalIgnoreCase))
        {
            var json = """{"isValid":true,"reason":null}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
