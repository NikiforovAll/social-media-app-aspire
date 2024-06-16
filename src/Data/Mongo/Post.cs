namespace Mongo;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    [BsonElement("CreatedAt")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset CreatedAt { get; set; }

    [BsonElement("Likes")]
    public List<int> Likes { get; private set; } = [];

    public void Like(int userId)
    {
        if (!this.Likes.Contains(userId))
        {
            this.Likes.Add(userId);
        }
    }

    public void RemoveLike(int userId) => this.Likes.Remove(userId);
}
