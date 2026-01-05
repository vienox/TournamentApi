namespace TournamentApi.Domain
{
    public class Tournament
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = "DRAFT";
        public Bracket? Bracket { get; set; }
        public List<TournamentParticipant> Participants { get; set; } = new();
    }
}