using BlazorApp.Models;
using BlazorApp.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace BlazorApp.Repositories.Implementations
{
    public class FormSubmissionCosmosRepository : IFormSubmissionRepository
    {
        private readonly Container _container;
        private string? _partitionKeyPath;
        private const string DatabaseName = "dynamicsdb";
        private const string ContainerName = "dynamics_submissions";

        public FormSubmissionCosmosRepository(CosmosClient cosmosClient)
        {
            _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
            // Don't perform any async/blocking work in the constructor. Partition key path will be
            // populated on-demand by EnsurePartitionKeyPathAsync so the repository construction is fast
            // and doesn't delay request handling.
            _partitionKeyPath = null;
        }

        public async Task<List<FormSubmission>> GetAllAsync()
        {
            // Returning all documents can be expensive. Use the iterator with a reasonable page
            // size so the first page can be returned quickly. Consider adding pagination in the UI
            // to avoid loading everything at once.
            var query = _container.GetItemQueryIterator<FormSubmission>(
                "SELECT * FROM c",
                requestOptions: new QueryRequestOptions { MaxItemCount = 100 });

            var results = new List<FormSubmission>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken)
        {
            QueryDefinition? qd = null;
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Use a more efficient search pattern: filter by exact matches first or use a composite index
                // CONTAINS is expensive because it performs a full-text scan. Consider:
                // 1. Denormalizing searchable fields at the root level
                // 2. Creating a composite index on (TextData.fullName, TextData.emailAddress)
                // For now, we'll use exact substring matching which is still indexed
                qd = new QueryDefinition(
                    @"SELECT * FROM c 
                      WHERE CONTAINS(UPPER(c.TextData.fullName), UPPER(@q)) 
                         OR CONTAINS(UPPER(c.TextData.emailAddress), UPPER(@q))")
                    .WithParameter("@q", search);
            }
            else
            {
                // For non-search queries, this still does a cross-partition query
                // Ideally, include partition key in WHERE clause if schema allows
                qd = new QueryDefinition("SELECT * FROM c");
            }

            var options = new QueryRequestOptions { MaxItemCount = pageSize };
            var it = _container.GetItemQueryIterator<FormSubmission>(qd, continuationToken, options);

            var page = await it.ReadNextAsync();
            return new PagedResult<FormSubmission> { Items = page.ToList(), ContinuationToken = page.ContinuationToken };
        }

        public async Task<FormSubmission?> GetByIdAsync(string id)
        {
            // Read by id using a query so we don't need to assume the partition key value
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id);
            var it = _container.GetItemQueryIterator<FormSubmission>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 });
            while (it.HasMoreResults)
            {
                var resp = await it.ReadNextAsync();
                if (resp.Count > 0) return resp.First();
            }
            return null;
        }

        public async Task SaveAsync(FormSubmission submission)
        {
            await EnsurePartitionKeyPathAsync();
            var pkValue = GetPartitionKeyValue(submission);
            if (pkValue == null)
            {
                throw new InvalidOperationException($"Partition key path '{_partitionKeyPath}' not found in document. Ensure the document contains the partition key value.");
            }
            await _container.UpsertItemAsync(submission, new PartitionKey(pkValue));
        }

        public async Task UpdateAsync(FormSubmission submission)
        {
            await EnsurePartitionKeyPathAsync();
            var pkValue = GetPartitionKeyValue(submission);
            if (pkValue == null)
            {
                throw new InvalidOperationException($"Partition key path '{_partitionKeyPath}' not found in document. Ensure the document contains the partition key value.");
            }
            await _container.UpsertItemAsync(submission, new PartitionKey(pkValue));
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                // Ensure we have the partition key path available
                await EnsurePartitionKeyPathAsync();

                // For Cosmos DB, we need the partition key value to delete an item.
                // The safest approach is to read the document first to ensure we have the correct partition key.
                var existing = await GetByIdAsync(id);
                if (existing == null)
                {
                    // Item doesn't exist, consider this a successful deletion
                    return;
                }

                // Get the partition key value from the retrieved document
                var pkValue = GetPartitionKeyValue(existing);
                if (pkValue == null)
                {
                    throw new InvalidOperationException(
                        $"Partition key path '{_partitionKeyPath}' not found in document with id '{id}'. Cannot delete item.");
                }

                // Delete the item using the ID and partition key
                await _container.DeleteItemAsync<FormSubmission>(id, new PartitionKey(pkValue));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Item doesn't exist - this is okay, treat as successful deletion
                return;
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                System.Diagnostics.Debug.WriteLine($"Delete failed for id '{id}': {ex.Message}");
                throw;
            }
        }

        private async Task EnsurePartitionKeyPathAsync()
        {
            if (!string.IsNullOrWhiteSpace(_partitionKeyPath)) return;

            var props = await _container.ReadContainerAsync();
            _partitionKeyPath = props.Resource.PartitionKeyPath;
        }

        private string? GetPartitionKeyValue(object item)
        {
            if (string.IsNullOrWhiteSpace(_partitionKeyPath) || _partitionKeyPath == "/") return null;

            // Serialize the object using System.Text.Json so any JsonPropertyName attributes are respected
            var json = JsonSerializer.Serialize(item);
            using var doc = JsonDocument.Parse(json);
            var segments = _partitionKeyPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            JsonElement element = doc.RootElement;
            foreach (var seg in segments)
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(seg, out element))
                {
                    return null;
                }
            }

            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => element.GetRawText(),
            };
        }
    }
}
