namespace V_Task.Models;

/// <summary>
/// Model for application settings stored in database
/// </summary>
public class AppSettings
{
    public int Id { get; set; } = 1;
    public string Language { get; set; } = "uk";
}
