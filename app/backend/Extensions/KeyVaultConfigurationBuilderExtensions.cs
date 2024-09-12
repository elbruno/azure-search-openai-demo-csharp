// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class KeyVaultConfigurationBuilderExtensions
{

    internal static IConfigurationBuilder ConfigureAzureKeyVault(this IConfigurationBuilder builder, string? azureKeyVaultEndpoint = null, string? tenantId = null)
    {
        // validate local development environment
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != Environments.Development)
        {
            // get KeyVault Endpoint
            azureKeyVaultEndpoint ??= Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT") ?? throw new InvalidOperationException("Azure Key Vault endpoint is not set.");
        }

        // need valid key vault endpoint
        ArgumentNullException.ThrowIfNullOrEmpty(azureKeyVaultEndpoint);

        // create Azure Credential
        var azureCredential = new DefaultAzureCredential();
        if (!string.IsNullOrEmpty(tenantId))
        {
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions { TenantId = tenantId };
            azureCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);
        }

        builder.AddAzureKeyVault(new Uri(azureKeyVaultEndpoint), azureCredential);

        return builder;
    }
}
