using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HobbyManagementApi.Models
{
    [BsonIgnoreExtraElements]
    public class HobbyItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("hobby_type")]
        public string HobbyType { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [BsonElement("rating")]
        public int Rating { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        // Chứa các thuộc tính động của từng loại sở thích
        [BsonElement("details")]
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updated_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}