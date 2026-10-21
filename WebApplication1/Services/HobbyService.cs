using HobbyManagementApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using WebApplication1.Models;

namespace HobbyManagementApi.Services
{
    public class HobbyService
    {
        private readonly IMongoCollection<HobbyItem> _hobbyCollection;

        public HobbyService(IOptions<MongoDbSettings> settings)
        {
            var mongoClient = new MongoClient(settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _hobbyCollection = mongoDatabase.GetCollection<HobbyItem>(settings.Value.CollectionName);
        }

        // 1. Lấy danh sách sở thích (Admin xem tất cả, User chỉ xem của mình)
        public async Task<List<HobbyItem>> GetAsync(string userId, string role = "User", string? hobbyType = null)
        {
            var filterBuilder = Builders<HobbyItem>.Filter;
            FilterDefinition<HobbyItem> filter;

            if (role == "Admin")
            {
                // Admin: Lấy toàn bộ dữ liệu
                filter = filterBuilder.Empty;
            }
            else
            {
                // User: Chỉ lấy dữ liệu do chính userId này sở hữu
                filter = filterBuilder.Eq(x => x.UserId, userId);
            }

            if (!string.IsNullOrEmpty(hobbyType))
            {
                filter &= filterBuilder.Eq(x => x.HobbyType, hobbyType);
            }

            return await _hobbyCollection.Find(filter).ToListAsync();
        }

        // 2. Xem chi tiết 1 sở thích theo ID
        public async Task<HobbyItem?> GetByIdAsync(string id) =>
            await _hobbyCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        // 3. Thêm mới sở thích (Chuyển đổi dữ liệu động Details)
        public async Task CreateAsync(HobbyItem newHobby)
        {
            newHobby.CreatedAt = DateTime.UtcNow;
            newHobby.UpdatedAt = DateTime.UtcNow;
            newHobby.Details = ProcessDetailsDictionary(newHobby.Details);

            await _hobbyCollection.InsertOneAsync(newHobby);
        }

        // 4. Cập nhật sở thích (Kiểm tra quyền Admin hoặc Chủ sở hữu)
        public async Task<bool> UpdateAsync(string id, HobbyItem updatedHobby, string currentUserId, string role = "User")
        {
            var existingItem = await GetByIdAsync(id);
            if (existingItem == null) return false;

            // Từ chối nếu không phải Admin và không phải chủ sở hữu
            if (role != "Admin" && existingItem.UserId != currentUserId)
            {
                return false;
            }

            updatedHobby.Id = existingItem.Id;
            updatedHobby.CreatedAt = existingItem.CreatedAt;
            updatedHobby.UpdatedAt = DateTime.UtcNow;
            updatedHobby.Details = ProcessDetailsDictionary(updatedHobby.Details);

            await _hobbyCollection.ReplaceOneAsync(x => x.Id == id, updatedHobby);
            return true;
        }

        // 5. Xóa sở thích (Kiểm tra quyền Admin hoặc Chủ sở hữu)
        public async Task<bool> RemoveAsync(string id, string currentUserId, string role = "User")
        {
            var existingItem = await GetByIdAsync(id);
            if (existingItem == null) return false;

            // Từ chối nếu không phải Admin và không phải chủ sở hữu
            if (role != "Admin" && existingItem.UserId != currentUserId)
            {
                return false;
            }

            await _hobbyCollection.DeleteOneAsync(x => x.Id == id);
            return true;
        }

        // 6. Thống kê Aggregation Pipeline
        public async Task<List<BsonDocument>> GetStatisticsAsync(string userId, string role = "User")
        {
            var matchStage = (role == "Admin" && string.IsNullOrEmpty(userId))
                ? new BsonDocument("$match", new BsonDocument())
                : new BsonDocument("$match", new BsonDocument("user_id", userId));

            var pipeline = new BsonDocument[]
            {
                matchStage,
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$hobby_type" },
                    { "total_items", new BsonDocument("$sum", 1) },
                    { "avg_rating", new BsonDocument("$avg", "$rating") }
                }),
                new BsonDocument("$sort", new BsonDocument("total_items", -1))
            };

            return await _hobbyCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
        }

        // HÀM HỖ TRỢ: Chuyển đổi linh hoạt JsonElement trong Details để lưu chuẩn vào MongoDB
        private Dictionary<string, object> ProcessDetailsDictionary(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, object>();
            if (dict == null) return result;

            foreach (var kvp in dict)
            {
                if (kvp.Value is JsonElement element)
                {
                    result[kvp.Key] = ConvertJsonElement(element);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? "";
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l)) return l;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(ConvertJsonElement).ToList();
                case JsonValueKind.Object:
                    var nestedDict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        nestedDict[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                    return nestedDict;
                default:
                    return element.ToString();
            }
        }
    }
}