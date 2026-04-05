using System.Net;
using System.Text;

namespace CareHub.Appointment.Tests.Helpers;

public sealed class FakePatientHandler : HttpMessageHandler
{
    public static readonly Guid MissingPatientId = Guid.Parse("00000000-0000-0000-0000-00000000dead");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.Contains(MissingPatientId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (path.Contains("/api/patients/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
