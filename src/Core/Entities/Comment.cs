using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Realworlddotnet.Core.Entities;

public class Comment(string body, string username, Guid articleId)
{
    public int Id { get; set; }

    [MaxLength(5000)]
    public string Body { get; set; } = body;

    [MaxLength(100)]
    public string Username { get; set; } = username;

    public Guid ArticleId { get; set; } = articleId;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Author { get; set; } = null!;
    [JsonIgnore]
    public Article Article { get; set; } = null!;
}
