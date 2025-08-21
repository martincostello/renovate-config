// Copyright (c) Martin Costello, 2025. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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

        AssertDescriptions(configuration, "$");
        AssertRegularExpressions(configuration, "$");
    }

    private static void AssertDescriptions(JToken token, string path)
    {
        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                var next = $"{path}/{property.Name}";

                if (property.Name is "description")
                {
                    property.Value.Type.ShouldBe(JTokenType.Array, next);
                    property.Value.ShouldAllBe(v => v.Type == JTokenType.String, next);
                }

                AssertDescriptions(property.Value, next);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            int index = 0;

            foreach (var item in token.Children())
            {
                AssertDescriptions(item, $"{path}[{index}]");
                index++;
            }
        }
    }

    private static void AssertRegularExpressions(JToken token, string path)
    {
        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                AssertRegularExpressions(property.Value, $"{path}/{property.Name}");
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            int index = 0;

            foreach (var item in token.Children())
            {
                AssertRegularExpressions(item, $"{path}[{index}]");
                index++;
            }
        }
        else if (token.Type == JTokenType.String)
        {
            var value = token.Value<string>();

            if (value?.Length > 2 && value[0] == '/' && value[^1] == '/')
            {
                Should.NotThrow(() => new Regex(value[1..^1]), $"Invalid regular expression at {path}: {value}");
            }
        }
    }
}
