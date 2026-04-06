using BlazorApp.Models;
using BlazorApp.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorApp.Repositories.Implementations
{
    /// <summary>
    /// Decorator pattern: wraps the actual repository with in-memory caching to reduce Cosmos DB hits.
    /// This significantly improves response times for frequently accessed data.
    /// </summary>
    public class CachedFormSubmissionRepository : IFormSubmissionRepository
    {
        private readonly IFormSubmissionRepository _innerRepository;
        private readonly IMemoryCache _cache;
        private const string AllItemsCacheKey = "form_submissions_all";
        private const string ItemByIdCacheKeyPrefix = "form_submission_";
        private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))  // Cache for 5 minutes
            .SetSlidingExpiration(TimeSpan.FromMinutes(2)); // Reset if accessed within 2 minutes

        public CachedFormSubmissionRepository(IFormSubmissionRepository innerRepository, IMemoryCache cache)
        {
            _innerRepository = innerRepository;
            _cache = cache;
        }

        public async Task<List<FormSubmission>> GetAllAsync()
        {
            const string cacheKey = AllItemsCacheKey;
            if (_cache.TryGetValue(cacheKey, out List<FormSubmission>? cachedItems))
            {
                return cachedItems!;
            }

            var items = await _innerRepository.GetAllAsync();
            _cache.Set(cacheKey, items, CacheOptions);
            return items;
        }

        public async Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken)
        {
            // Don't cache paginated results with continuation tokens as they're stateful
            // Only cache the initial page (no continuation token)
            if (!string.IsNullOrWhiteSpace(continuationToken) || !string.IsNullOrWhiteSpace(search))
            {
                return await _innerRepository.GetPagedAsync(search, pageSize, continuationToken);
            }

            const string cacheKey = "form_submissions_first_page";
            if (_cache.TryGetValue(cacheKey, out PagedResult<FormSubmission>? cachedResult))
            {
                return cachedResult!;
            }

            var result = await _innerRepository.GetPagedAsync(search, pageSize, continuationToken);
            _cache.Set(cacheKey, result, CacheOptions);
            return result;
        }

        public async Task<FormSubmission?> GetByIdAsync(string id)
        {
            var cacheKey = $"{ItemByIdCacheKeyPrefix}{id}";
            if (_cache.TryGetValue(cacheKey, out FormSubmission? cachedItem))
            {
                return cachedItem;
            }

            var item = await _innerRepository.GetByIdAsync(id);
            if (item != null)
            {
                _cache.Set(cacheKey, item, CacheOptions);
            }
            return item;
        }

        public async Task SaveAsync(FormSubmission submission)
        {
            await _innerRepository.SaveAsync(submission);
            InvalidateCache();
        }

        public async Task UpdateAsync(FormSubmission submission)
        {
            await _innerRepository.UpdateAsync(submission);
            InvalidateCache(submission.Id);
        }

        public async Task DeleteAsync(string id)
        {
            await _innerRepository.DeleteAsync(id);
            InvalidateCache(id);
        }

        private void InvalidateCache(string? specificId = null)
        {
            // Invalidate the all-items cache
            _cache.Remove(AllItemsCacheKey);
            _cache.Remove("form_submissions_first_page");

            // Invalidate specific item cache if provided
            if (!string.IsNullOrWhiteSpace(specificId))
            {
                _cache.Remove($"{ItemByIdCacheKeyPrefix}{specificId}");
            }
        }
    }
}
