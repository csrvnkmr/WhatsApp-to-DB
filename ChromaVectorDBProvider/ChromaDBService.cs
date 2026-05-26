using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace ChromaVectorDBProvider
{
    public class ChromaDBService : IVectorDBService
    {
        private const string ProviderVersion = "ChromaVectorDBProvider v2-rest-only-2026-05-24-1455";

        private readonly HttpClient _httpClient;
        private readonly VectorDBSettings _vectorDBSettings;
        private readonly string _baseUrl;
        private bool _useConfiguredNamespace = true;

        public ChromaDBService(VectorDBSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var chromaUrl = settings.VectorDBProviderSettings.Url
                ?? throw new InvalidOperationException("Chroma URL is not configured.");

            Console.WriteLine($"[{ProviderVersion}] ctor start");
            Console.WriteLine($"[{ProviderVersion}] ChromaUrl={chromaUrl}");
            Console.WriteLine($"[{ProviderVersion}] TenantName={settings.VectorDBProviderSettings.TenantName ?? "<null>"} Database={settings.VectorDBProviderSettings.Database ?? "<null>"}");

            _httpClient = new HttpClient();
            _vectorDBSettings = settings;
            _baseUrl = NormalizeBaseUrl(chromaUrl);

            Console.WriteLine($"[{ProviderVersion}] ctor complete");
        }

        public async Task Add(string collectionName, List<string> ids, List<ReadOnlyMemory<float>> vectors,
            List<string>? documents, List<Dictionary<string, object>>? metadatas)
        {
            Console.WriteLine($"[{ProviderVersion}] Add collection={collectionName} ids={ids?.Count ?? 0}");
            await EnsureNamespaceAsync();

            // This now returns the required string UUID (e.g., "797e91e3-8c34-4ce7-b294-0c9eb3eebdb4")
            var collectionId = await EnsureCollectionExistsAsync(NormalizeCollectionName(collectionName));

            var body = new
            { 
                ids,
                documents,
                metadatas,
                embeddings = vectors.Select(e => e.ToArray()).ToArray()
            };

            // The endpoint path now successfully receives a valid UUIDv4 string
            await SendAsync(
                HttpMethod.Post,
                $"/api/v2/tenants/{Uri.EscapeDataString(GetTenantName())}/databases/{Uri.EscapeDataString(GetDatabaseName())}/collections/{Uri.EscapeDataString(collectionId)}/add",
                body,
                expectedStatusCodes: new[] { HttpStatusCode.Created, HttpStatusCode.OK });
        }

        public async Task Addold(string collectionName, List<string> ids, List<ReadOnlyMemory<float>> vectors,
            List<string>? documents, List<Dictionary<string, object>>? metadatas)
        {
            Console.WriteLine($"[{ProviderVersion}] Add collection={collectionName} ids={ids?.Count ?? 0}");
            await EnsureNamespaceAsync();

            var chromaCollectionName = await EnsureCollectionExistsAsync(NormalizeCollectionName(collectionName));

            var body = new
            {
                ids,
                documents,
                metadatas,
                embeddings = vectors.Select(e => e.ToArray()).ToArray()
            };

            await SendAsync(
                HttpMethod.Post,
                $"/api/v2/tenants/{Uri.EscapeDataString(GetTenantName())}/databases/{Uri.EscapeDataString(GetDatabaseName())}/collections/{Uri.EscapeDataString(chromaCollectionName)}/add",
                body,
                expectedStatusCodes: new[] { HttpStatusCode.Created, HttpStatusCode.OK });
        }

        public async Task Delete(string collectionName)
        {
            Console.WriteLine($"[{ProviderVersion}] Delete full collection schema={collectionName}");
            try
            {
                
            await EnsureNamespaceAsync();

            // Use your string resolver directly since the administrative endpoint expects the text name
            var effectiveTextName = GetEffectiveCollectionName(NormalizeCollectionName(collectionName));
            
            await SendAsync(
                HttpMethod.Delete,
                $"/api/v2/tenants/{Uri.EscapeDataString(GetTenantName())}/databases/{Uri.EscapeDataString(GetDatabaseName())}/collections/{Uri.EscapeDataString(effectiveTextName)}",
                null,
                expectedStatusCodes: new[] { HttpStatusCode.OK });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{ProviderVersion}] Error deleting collection '{collectionName}': {ex}");                                
            }
        }

        public async Task Delete(string collectionName, string id)
        {
            Console.WriteLine($"[{ProviderVersion}] Delete record collection={collectionName} id={id}");
            await EnsureNamespaceAsync();

            // Change: Extracts the strict backend UUID context
            var collectionId = await EnsureCollectionExistsAsync(NormalizeCollectionName(collectionName));

            var body = new { ids = new[] { id } };
            await SendAsync(
                HttpMethod.Post,
                $"/api/v2/tenants/{Uri.EscapeDataString(GetTenantName())}/databases/{Uri.EscapeDataString(GetDatabaseName())}/collections/{Uri.EscapeDataString(collectionId)}/delete",
                body,
                expectedStatusCodes: new[] { HttpStatusCode.OK });
        }

       public async Task<List<VectorSearchResult>> SearchCollection(
            string collectionName,
            ReadOnlyMemory<float> queryVector,
            string? queryText = null,
            int limit = 5,
            IDictionary<string, object>? filter = null)
        {
            Console.WriteLine($"[{ProviderVersion}] Search collection={collectionName} queryVectorLength={queryVector.Length}");
            await EnsureNamespaceAsync();

            // Change: EnsureCollectionExistsAsync now returns the GUID string!
            var collectionId = await EnsureCollectionExistsAsync(NormalizeCollectionName(collectionName));

            var queryEmbeddings = new[] { queryVector.ToArray() };

            object? whereFilter = null;
            if (filter != null && filter.Any())
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kvp in filter)
                {
                    dict[kvp.Key] = kvp.Value;
                }
                whereFilter = dict;
            }

            var body = new
            {
                query_embeddings = queryEmbeddings,
                n_results = limit,
                where = whereFilter,
                include = new[] { "metadatas", "distances", "documents" }
            };

            // Interpolation matches your clean data lookup path via UUID string
            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/v2/tenants/{Uri.EscapeDataString(GetTenantName())}/databases/{Uri.EscapeDataString(GetDatabaseName())}/collections/{Uri.EscapeDataString(collectionId)}/query",
                body,
                expectedStatusCodes: new[] { HttpStatusCode.OK });

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<ChromaQueryResponse>(json, JsonOptions);
            var firstBatch = parsed?.Ids?.FirstOrDefault();
            if (firstBatch == null)
            {
                return new List<VectorSearchResult>();
            }

            var result = new List<VectorSearchResult>();
            for (var i = 0; i < firstBatch.Count; i++)
            {
                result.Add(new VectorSearchResult
                {
                    Id = parsed?.Metadatas?.FirstOrDefault()?.ElementAtOrDefault(i)?.TryGetValue("_source_id", out var sourceId) == true
                        ? sourceId?.ToString() ?? firstBatch[i]
                        : firstBatch[i],
                    Document = parsed?.Documents?.FirstOrDefault()?.ElementAtOrDefault(i),
                    Distance = parsed?.Distances?.FirstOrDefault()?.ElementAtOrDefault(i) is double distance
                        ? (float?)distance
                        : null,
                    Metadata = parsed?.Metadatas?.FirstOrDefault()?.ElementAtOrDefault(i)
                });
            }

            return result;
        }

        private async Task EnsureNamespaceAsync()
        {
            if (!_useConfiguredNamespace)
            {
                return;
            }

            var tenant = GetTenantName();
            var database = GetDatabaseName();

            Console.WriteLine($"[{ProviderVersion}] Ensuring tenant '{tenant}' and database '{database}' exist.");
            var tenantOk = await EnsureTenantAsync(tenant);
            var databaseOk = tenantOk && await EnsureDatabaseAsync(tenant, database);

            if (!tenantOk || !databaseOk)
            {
                _useConfiguredNamespace = false;
                Console.WriteLine($"[{ProviderVersion}] Chroma admin namespace creation is unavailable. Falling back to default_tenant/default_database with collection prefixing.");
            }
        }

        private string GetTenantName()
        {
            return _vectorDBSettings.VectorDBProviderSettings.TenantName?.Trim()
                   ?? throw new InvalidOperationException("Chroma tenant name is not configured.");
        }

        private string GetDatabaseName()
        {
            return _vectorDBSettings.VectorDBProviderSettings.Database?.Trim()
                   ?? throw new InvalidOperationException("Chroma database name is not configured.");
        }

        private async Task<bool> EnsureTenantAsync(string tenant)
        {
            var path = $"/api/v2/tenants/{Uri.EscapeDataString(tenant)}";
            Console.WriteLine($"[{ProviderVersion}] EnsureTenant GET {_baseUrl}{path}");

            using var getResponse = await SendAsync(HttpMethod.Get, path, null, expectedStatusCodes: new[] { HttpStatusCode.OK }, allowStatusCodes: new[] { HttpStatusCode.NotFound });
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            Console.WriteLine($"[{ProviderVersion}] Creating tenant '{tenant}'.");
            try
            {
                await SendAsync(
                    HttpMethod.Post,
                    "/api/v2/tenants",
                    new { name = tenant },
                    expectedStatusCodes: new[] { HttpStatusCode.OK });
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[{ProviderVersion}] Tenant bootstrap failed for '{tenant}': {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnsureDatabaseAsync(string tenant, string database)
        {
            var path = $"/api/v2/tenants/{Uri.EscapeDataString(tenant)}/databases/{Uri.EscapeDataString(database)}";
            Console.WriteLine($"[{ProviderVersion}] EnsureDatabase GET {_baseUrl}{path}");

            using var getResponse = await SendAsync(HttpMethod.Get, path, null, expectedStatusCodes: new[] { HttpStatusCode.OK }, allowStatusCodes: new[] { HttpStatusCode.NotFound });
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            Console.WriteLine($"[{ProviderVersion}] Creating database '{database}' under tenant '{tenant}'.");
            try
            {
                await SendAsync(
                    HttpMethod.Post,
                    $"/api/v2/tenants/{Uri.EscapeDataString(tenant)}/databases",
                    new { name = database },
                    expectedStatusCodes: new[] { HttpStatusCode.OK });
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[{ProviderVersion}] Database bootstrap failed for '{database}': {ex.Message}");
                return false;
            }
        }

       private async Task<string> EnsureCollectionExistsAsync(string collectionName)
        {
            var tenant = GetTenantName();
            var database = GetDatabaseName();
            var effectiveCollectionName = GetEffectiveCollectionName(collectionName);
            var path = $"/api/v2/tenants/{Uri.EscapeDataString(tenant)}/databases/{Uri.EscapeDataString(database)}/collections/{Uri.EscapeDataString(effectiveCollectionName)}";
            Console.WriteLine($"[{ProviderVersion}] EnsureCollection GET {_baseUrl}{path}");

            using var getResponse = await SendAsync(HttpMethod.Get, path, null, expectedStatusCodes: new[] { HttpStatusCode.OK }, allowStatusCodes: new[] { HttpStatusCode.NotFound });
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                var json = await getResponse.Content.ReadAsStringAsync();
                var collectionData = JsonSerializer.Deserialize<ChromaCollectionResponse>(json, JsonOptions);
                return collectionData?.Id ?? throw new InvalidOperationException($"Could not extract UUID for collection '{effectiveCollectionName}'.");
            }

            Console.WriteLine($"[{ProviderVersion}] Creating collection '{effectiveCollectionName}'.");
            try
            {
                var postResponse = await SendAsync(
                    HttpMethod.Post,
                    $"/api/v2/tenants/{Uri.EscapeDataString(tenant)}/databases/{Uri.EscapeDataString(database)}/collections",
                    new { name = effectiveCollectionName },
                    expectedStatusCodes: new[] { HttpStatusCode.OK, HttpStatusCode.Created });
                    
                var json = await postResponse.Content.ReadAsStringAsync();
                var collectionData = JsonSerializer.Deserialize<ChromaCollectionResponse>(json, JsonOptions);
                return collectionData?.Id ?? throw new InvalidOperationException($"Could not parse generated UUID for new collection '{effectiveCollectionName}'.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound && _useConfiguredNamespace)
            {
                Console.WriteLine($"[{ProviderVersion}] Collection create returned 404 for configured namespace. Falling back to default namespace with collection prefixing.");
                _useConfiguredNamespace = false;
                return await EnsureCollectionExistsAsync(collectionName);
            }
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            object? body,
            HttpStatusCode[] expectedStatusCodes,
            HttpStatusCode[]? allowStatusCodes = null)
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
            if (body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            if (expectedStatusCodes.Contains(response.StatusCode) || (allowStatusCodes != null && allowStatusCodes.Contains(response.StatusCode)))
            {
                return response;
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[{ProviderVersion}] HTTP {method.Method} {_baseUrl}{path} -> {(int)response.StatusCode} {response.StatusCode}");
            if (!string.IsNullOrWhiteSpace(content))
            {
                Console.WriteLine($"[{ProviderVersion}] Response body: {content}");
            }
            response.EnsureSuccessStatusCode();
            throw new InvalidOperationException($"Unexpected Chroma response {(int)response.StatusCode}: {content}");
        }

        private static string NormalizeCollectionName(string collectionName)
        {
            var normalized = collectionName.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"[^a-z0-9._-]+", "-");
            normalized = Regex.Replace(normalized, @"[-_.]{2,}", "-");
            normalized = normalized.Trim('-', '.', '_');

            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "collection";
            }

            if (normalized.Length < 3)
            {
                normalized = normalized.PadRight(3, '0');
            }

            return normalized;
        }

        private static string NormalizeBaseUrl(string url)
        {
            var normalized = url.Trim().TrimEnd('/');
            const string apiV2Suffix = "/api/v2";
            if (normalized.EndsWith(apiV2Suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^apiV2Suffix.Length];
            }

            return normalized.TrimEnd('/');
        }

        private string GetEffectiveCollectionName(string collectionName)
        {
            if (_useConfiguredNamespace)
            {
                return collectionName;
            }

            var sourcePrefix = NormalizeCollectionName(GetDatabaseName());
            var prefixed = NormalizeCollectionName($"{sourcePrefix}-{collectionName}");
            Console.WriteLine($"[{ProviderVersion}] Using fallback collection name '{prefixed}' for source database '{GetDatabaseName()}'.");
            return prefixed;
        }

        private sealed class ChromaQueryResponse
        {
            public List<List<string>>? Ids { get; set; }
            public List<List<string?>>? Documents { get; set; }
            public List<List<double?>>? Distances { get; set; }
            public List<List<Dictionary<string, object>>?>? Metadatas { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private sealed class ChromaCollectionResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
    
}
