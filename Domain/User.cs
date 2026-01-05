namespace TournamentApi.Domain
{
    public class User
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public List<TournamentParticipant> TournamentParticipants { get; set; } = new();
    }
}