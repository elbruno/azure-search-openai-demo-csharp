// Copyright (c) Microsoft. All rights reserved.

using Shared.Config;

namespace MinimalApi.Extensions;

internal static class KeyVaultConfigurationBuilderExtensions
{
    internal static WebApplicationBuilder ConfigureAzureKeyVault(this WebApplicationBuilder builder)
    {

        var azureKeyVaultEndpoint = builder.Configuration[ConfigKeys.AZURE_KEY_VAULT_ENDPOINT];
        var tenantId = builder.Configuration[ConfigKeys.AZURE_TENANT_ID];

        // validate local development environment
        if (Environment.GetEnvironmentVariable(ConfigKeys.ASPNETCORE_ENVIRONMENT) != Environments.Development)
        {
            // get KeyVault Endpoint
            azureKeyVaultEndpoint ??= Environment.GetEnvironmentVariable(ConfigKeys.AZURE_KEY_VAULT_ENDPOINT) ?? throw new InvalidOperationException("Azure Key Vault endpoint is not set.");
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

        builder.Configuration.AddAzureKeyVault(new Uri(azureKeyVaultEndpoint), azureCredential);


        return builder;
    }
}
