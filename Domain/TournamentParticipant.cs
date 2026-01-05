namespace TournamentApi.Domain
{
    public class TournamentParticipant
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}