using Xunit;

namespace CareHub.Document.Tests;

/// <summary>
/// Serializes Document tests that touch QuestPDF / file storage so parallel runs do not flake.
/// </summary>
[CollectionDefinition("Document integration", DisableParallelization = true)]
public sealed class DocumentIntegrationCollection;
