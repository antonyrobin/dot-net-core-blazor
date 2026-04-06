using System.Text.RegularExpressions;
using BlazorApp.Models;
using BlazorApp.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazorApp.Repositories.Implementations;

public class FormSubmissionMongoRepository : IFormSubmissionRepository
{
    private readonly IMongoCollection<FormSubmission> _collection;

    public FormSubmissionMongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<FormSubmission>("dynamics");
    }

    public async Task<List<FormSubmission>> GetAllAsync()
    {
        // Warning: can be very expensive — consider pagination in production
        return await _collection.Find(FilterDefinition<FormSubmission>.Empty)
            .ToListAsync();
    }

    public async Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken)
    {
        var filter = Builders<FormSubmission>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var regex = new BsonRegularExpression(Regex.Escape(search), "i"); // case-insensitive

            filter = Builders<FormSubmission>.Filter.Or(
                Builders<FormSubmission>.Filter.Regex("TextData.fullName", regex),
                Builders<FormSubmission>.Filter.Regex("TextData.emailAddress", regex)
            );
        }

        // For very large collections consider using proper text index + $text search
        // db.dynamics_submissions.createIndex({ "TextData.fullName": "text", "TextData.emailAddress": "text" })

        var totalTask = _collection.CountDocumentsAsync(filter);

        var find = _collection.Find(filter)
            //.SortByDescending(x => x.CreatedAt ?? DateTime.MinValue) // optional — adjust sort
            .Limit(pageSize);

        // Very basic continuation (skip-based — not efficient for large offsets)
        if (!string.IsNullOrEmpty(continuationToken) && int.TryParse(continuationToken, out var skip))
        {
            find = find.Skip(skip);
        }

        var items = await find.ToListAsync();
        var total = await totalTask;

        string? nextToken = null;
        if (items.Count == pageSize)
        {
            var nextSkip = (0 + pageSize);
            nextToken = nextSkip.ToString();
        }

        return new PagedResult<FormSubmission>
        {
            Items = items,
            ContinuationToken = nextToken,
            // Optional: TotalCount = total  (if your PagedResult supports it)
        };
    }

    public async Task<FormSubmission?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveAsync(FormSubmission submission)
    {
        // Assuming Id is string and filled (guid or whatever your model uses)
        if (string.IsNullOrEmpty(submission.Id))
        {
            submission.Id = ObjectId.GenerateNewId().ToString();
        }

        await _collection.InsertOneAsync(submission);
    }

    public async Task UpdateAsync(FormSubmission submission)
    {
        if (string.IsNullOrEmpty(submission.Id))
            throw new ArgumentException("Id is required for update");

        await _collection.ReplaceOneAsync(
            x => x.Id == submission.Id,
            submission,
            new ReplaceOptions { IsUpsert = false });
    }

    public async Task DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return;

        await _collection.DeleteOneAsync(x => x.Id == id);
    }
}