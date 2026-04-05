using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Document.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Document.Tests;

public class DocumentApiTests : IClassFixture<DocumentTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly DocumentTestFactory _factory;

    public DocumentApiTests(DocumentTestFactory factory)
    {
        _factory = factory;
        _ = _factory.Server;
    }

    [Fact]
    public async Task Generate_Referral_Then_List_Then_Download_RoundTrip()
    {
        var entityId = Guid.NewGuid();
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var gen = await client.PostAsJsonAsync(
            "/api/documents/generate",
            new { template = "Referral", entityId, branchId = DocumentTestFactory.DefaultBranchId });
        gen.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var list = await client.GetAsync(
            $"/api/documents?entityType=Referral&entityId={entityId:D}&page=1&pageSize=10");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await list.Content.ReadFromJsonAsync<ListResponse>(JsonOptions);
        listBody.Should().NotBeNull();
        listBody!.Total.Should().Be(1);
        var docId = listBody.Items[0].Id;

        var pdf = await client.GetAsync($"/api/documents/{docId:D}");
        pdf.StatusCode.Should().Be(HttpStatusCode.OK);
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await pdf.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task List_Missing_Query_Returns_BadRequest()
    {
        var client = _factory.CreateClient();
        var r = await client.GetAsync("/api/documents");
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ListResponse(int Page, int PageSize, int Total, List<Item> Items);

    private sealed record Item(Guid Id, string Kind, string FileName, string EntityType, Guid EntityId, Guid? BranchId, DateTime CreatedAt, string Source);
}
