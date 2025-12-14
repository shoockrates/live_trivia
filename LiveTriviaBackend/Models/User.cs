using System.ComponentModel.DataAnnotations;
namespace live_trivia;

public class User : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int? PlayerId { get; set; }

    public virtual Player? Player { get; set; }

    public bool IsAdmin { get; set; } = false;
}
