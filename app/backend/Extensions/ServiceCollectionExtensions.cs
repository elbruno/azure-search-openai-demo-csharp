// Copyright (c) Microsoft. All rights reserved.

using OpenAI;
using Shared.Config;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static WebApplicationBuilder AddAzureServices(this WebApplicationBuilder builder)
    {
        // by def we use a default azure credential
        var s_azureCredential = new DefaultAzureCredential();

        if (Environment.GetEnvironmentVariable(ConfigKeys.ASPNETCORE_ENVIRONMENT) == Environments.Development)
        {
            var azureKeyVaultEndpoint = builder.Configuration[ConfigKeys.AZURE_KEY_VAULT_ENDPOINT];
            var tenantId = builder.Configuration[ConfigKeys.AZURE_TENANT_ID];

            // on dev scenarios, we use the info from the appsettings.development.json
            var da = new DefaultAzureCredentialOptions
            {
                TenantId = tenantId
            };
            s_azureCredential = new DefaultAzureCredential(da);
        }

        builder.Services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountEndpoint = config[ConfigKeys.AzureStorageAccountEndpoint];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(
                new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        builder.Services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config[ConfigKeys.AzureStorageContainer];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        builder.Services.AddSingleton<ISearchService, AzureSearchService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureSearchServiceEndpoint = config[ConfigKeys.AzureSearchServiceEndpoint];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var azureSearchIndex = config[ConfigKeys.AzureSearchIndex];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchIndex);

            var searchClient = new SearchClient(
                               new Uri(azureSearchServiceEndpoint), azureSearchIndex, s_azureCredential);

            return new AzureSearchService(searchClient);
        });

        builder.Services.AddSingleton<DocumentAnalysisClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config[ConfigKeys.AzureOpenAiServiceEndpoint] ?? throw new ArgumentNullException();

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);
            return documentAnalysisClient;
        });

        builder.Services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var use_AOAI = config[ConfigKeys.USE_AOAI] == "true";
            if (use_AOAI)
            {
                var azureOpenAiServiceEndpoint = config[ConfigKeys.AzureOpenAiServiceEndpoint];
                ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

                var openAIClient = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint), s_azureCredential);

                return openAIClient;
            }
            else
            {
                var openAIApiKey = config[ConfigKeys.OpenAIApiKey];
                ArgumentNullException.ThrowIfNullOrEmpty(openAIApiKey);

                var openAIClient = new OpenAIClient(openAIApiKey);
                return openAIClient;
            }
        });

        builder.Services.AddSingleton<AzureBlobStorageService>();
        builder.Services.AddSingleton<ReadRetrieveReadChatService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var useVision = config[ConfigKeys.UseVision] == "true";
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            var searchClient = sp.GetRequiredService<ISearchService>();
            if (useVision)
            {
                var azureComputerVisionServiceEndpoint = config[ConfigKeys.AzureComputerVisionServiceEndpoint];
                ArgumentNullException.ThrowIfNullOrEmpty(azureComputerVisionServiceEndpoint);
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();

                var visionService = new AzureComputerVisionService(httpClient, azureComputerVisionServiceEndpoint, s_azureCredential);
                return new ReadRetrieveReadChatService(searchClient, openAIClient, config, visionService, s_azureCredential);
            }
            else
            {
                return new ReadRetrieveReadChatService(searchClient, openAIClient, config, tokenCredential: s_azureCredential);
            }
        });

        return builder;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
