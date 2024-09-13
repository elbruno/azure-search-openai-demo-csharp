// Copyright (c) Microsoft. All rights reserved.

using Shared.Config;

namespace MinimalApi.Extensions;

internal static class ConfigurationExtensions
{
    internal static string GetStorageAccountEndpoint(this IConfiguration config)
    {
        var endpoint = config[ConfigKeys.AzureStorageAccountEndpoint];
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        return endpoint;
    }

    internal static string ToCitationBaseUrl(this IConfiguration config)
    {
        var endpoint = config.GetStorageAccountEndpoint();

        var builder = new UriBuilder(endpoint)
        {
            Path = config[ConfigKeys.AzureStorageContainer]
        };

        return builder.Uri.AbsoluteUri;
    }
}
