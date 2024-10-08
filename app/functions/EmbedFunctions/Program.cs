﻿// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
using OpenAI;
using Shared.Config;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        var credential = new DefaultAzureCredential();

        static Uri GetUriFromEnvironment(string variable) => Environment.GetEnvironmentVariable(variable) is string value &&
                Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
                uri is not null
                ? uri
                : throw new ArgumentException(
                $"Unable to parse URI from environment variable: {variable}");

        services.AddAzureClients(builder =>
        {
            builder.AddDocumentAnalysisClient(
                GetUriFromEnvironment(ConfigKeys.AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT));
        });

        services.AddSingleton<SearchClient>(_ =>
        {
            return new SearchClient(
                GetUriFromEnvironment(ConfigKeys.AZURE_SEARCH_SERVICE_ENDPOINT),
                Environment.GetEnvironmentVariable(ConfigKeys.AZURE_SEARCH_INDEX),
                credential);
        });

        services.AddSingleton<SearchIndexClient>(_ =>
        {
            return new SearchIndexClient(
                GetUriFromEnvironment(ConfigKeys.AZURE_SEARCH_SERVICE_ENDPOINT),
                credential);
        });

        services.AddSingleton<BlobContainerClient>(_ =>
        {
            var blobServiceClient = new BlobServiceClient(
                GetUriFromEnvironment(ConfigKeys.AZURE_STORAGE_BLOB_ENDPOINT),
                credential);

            var containerClient = blobServiceClient.GetBlobContainerClient("corpus");

            containerClient.CreateIfNotExists();

            return containerClient;
        });

        services.AddSingleton<BlobServiceClient>(_ =>
        {
            return new BlobServiceClient(
                GetUriFromEnvironment(ConfigKeys.AZURE_STORAGE_BLOB_ENDPOINT), credential);
        });

        services.AddSingleton<EmbedServiceFactory>();
        services.AddSingleton<EmbeddingAggregateService>();

        services.AddSingleton<IEmbedService, AzureSearchEmbedService>(provider =>
        {
            var searchIndexName = Environment.GetEnvironmentVariable(ConfigKeys.AZURE_SEARCH_INDEX) ?? throw new ArgumentNullException("AZURE_SEARCH_INDEX is null");
            var USEAOAI = Environment.GetEnvironmentVariable(ConfigKeys.USEAOAI)?.ToLower() == "true";
            var useVision = Environment.GetEnvironmentVariable(ConfigKeys.USE_VISION)?.ToLower() == "true";

            OpenAIClient? openAIClient = null;
            string? embeddingModelName = null;

            if (USEAOAI)
            {
                var openaiEndPoint = Environment.GetEnvironmentVariable(ConfigKeys.AZURE_OPENAI_ENDPOINT) ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT is null");
                embeddingModelName = Environment.GetEnvironmentVariable(ConfigKeys.AZURE_OPENAI_EMBEDDING_DEPLOYMENT) ?? throw new ArgumentNullException("AZURE_OPENAI_EMBEDDING_DEPLOYMENT is null");
                openAIClient = new AzureOpenAIClient(new Uri(openaiEndPoint), new DefaultAzureCredential());
            }
            else
            {
                embeddingModelName = Environment.GetEnvironmentVariable(ConfigKeys.OPENAI_EMBEDDING_DEPLOYMENT) ?? throw new ArgumentNullException("OPENAI_EMBEDDING_DEPLOYMENT is null");
                var openaiKey = Environment.GetEnvironmentVariable(ConfigKeys.OPENAI_API_KEY) ?? throw new ArgumentNullException("OPENAI_API_KEY is null");
                openAIClient = new OpenAIClient(openaiKey);
            }

            var searchClient = provider.GetRequiredService<SearchClient>();
            var searchIndexClient = provider.GetRequiredService<SearchIndexClient>();
            var corpusContainer = provider.GetRequiredService<BlobContainerClient>();
            var documentClient = provider.GetRequiredService<DocumentAnalysisClient>();
            var logger = provider.GetRequiredService<ILogger<AzureSearchEmbedService>>();

            if (useVision)
            {
                var visionEndpoint = Environment.GetEnvironmentVariable(ConfigKeys.AZURE_COMPUTER_VISION_ENDPOINT) ?? throw new ArgumentNullException("AZURE_COMPUTER_VISION_ENDPOINT is null");
                var httpClient = new HttpClient();
                var visionClient = new AzureComputerVisionService(httpClient, visionEndpoint, new DefaultAzureCredential());

                return new AzureSearchEmbedService(
                    openAIClient: openAIClient,
                    embeddingModelName: embeddingModelName,
                    searchClient: searchClient,
                    searchIndexName: searchIndexName,
                    searchIndexClient: searchIndexClient,
                    documentAnalysisClient: documentClient,
                    corpusContainerClient: corpusContainer,
                    computerVisionService: visionClient,
                    includeImageEmbeddingsField: true,
                    logger: logger);
            }
            else
            {
                return new AzureSearchEmbedService(
                openAIClient: openAIClient,
                embeddingModelName: embeddingModelName,
                searchClient: searchClient,
                searchIndexName: searchIndexName,
                searchIndexClient: searchIndexClient,
                documentAnalysisClient: documentClient,
                corpusContainerClient: corpusContainer,
                computerVisionService: null,
                includeImageEmbeddingsField: false,
                logger: logger);
            }
        });
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
