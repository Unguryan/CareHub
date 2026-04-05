namespace CareHub.Document.Options;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";
    public string RootPath { get; set; } = "./data/documents";
}

public class DocumentFeatureOptions
{
    public const string SectionName = "Document";
    public bool UseLaboratoryInternalApi { get; set; } = true;
    public string LaboratoryFallbackMessage { get; set; } =
        "See the laboratory system for the full result.";
}

public class LaboratoryInternalOptions
{
    public const string SectionName = "LaboratoryInternal";
    public string BaseUrl { get; set; } = "http://localhost:5006";
    public string ApiKey { get; set; } = "";
}
