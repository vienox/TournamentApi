namespace TournamentApi.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "TournamentApi";
    public string Audience { get; set; } = "TournamentApi";
    public string Key { get; set; } = "secretkey123";
    public int ExpMinutes { get; set; } = 60;
}
