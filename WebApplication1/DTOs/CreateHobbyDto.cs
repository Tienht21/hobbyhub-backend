using System.Text.Json;

namespace HobbyHub.DTOs
{
    public class CreateHobbyDto
    {
        public string UserId { get; set; } = string.Empty;
        public string HobbyType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Rating { get; set; }
        public List<string> Tags { get; set; } = new();
        public JsonElement Details { get; set; }
    }
}