﻿namespace Mongo;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string ExternalId { get; set; } = default!;

    [BsonElement("CreatedAt")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset CreatedAt { get; set; }

    [BsonElement("Likes")]
    public List<int> Likes { get; private set; } = [];

    public bool Like(int userId)
    {
        if (!this.Likes.Contains(userId))
        {
            this.Likes.Add(userId);

            return true;
        }

        return false;
    }

    public void RemoveLike(int userId) => this.Likes.Remove(userId);
}
