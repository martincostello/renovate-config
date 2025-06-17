// Copyright (c) Martin Costello, 2025. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace MartinCostello.Renovate.Schema;

public static class RenovateSchemaTests
{
    [Fact]
    public static async Task Renovate_Configuration_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var configurationFile = "org-inherited-config.json";
        var schemaUrl = "https://docs.renovatebot.com/renovate-schema.json";

        var directory = new DirectoryInfo(".");

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, configurationFile)))
        {
            directory = directory.Parent;
        }

        configurationFile = Path.Combine(directory?.FullName ?? ".", configurationFile);
        File.Exists(configurationFile).ShouldBeTrue();

        using var client = new HttpClient();
        var schemaJson = await client.GetStringAsync(schemaUrl, cancellationToken);
        var schema = JSchema.Parse(
            schemaJson,
            new JSchemaReaderSettings() { ValidateVersion = true });

        var configurationJson = await File.ReadAllTextAsync(configurationFile, cancellationToken);
        var configuration = JToken.Parse(configurationJson);

        // Act
        var actual = configuration.IsValid(schema, out IList<string> errors);

        // Assert
        errors.ShouldNotBeNull();
        errors.ShouldBeEmpty();
        actual.ShouldBeTrue();
    }
}
