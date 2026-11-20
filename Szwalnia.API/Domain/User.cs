namespace Szwalnia.API;

public class User : Entity
{
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = UserRole.Cutting;
}
