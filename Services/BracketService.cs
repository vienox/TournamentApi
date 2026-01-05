using Microsoft.EntityFrameworkCore;
using TournamentApi.Data;
using TournamentApi.Domain;

namespace TournamentApi.Services;

public class BracketService
{
    private readonly AppDbContext _db;

    public BracketService(AppDbContext db) => _db = db;

    public async Task<Bracket> GenerateBracketAsync(int tournamentId)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Participants).ThenInclude(tp => tp.User)
            .Include(t => t.Bracket).ThenInclude(b => b!.Matches)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament is null) throw new Exception("Tournament not found");
        if (tournament.Bracket != null && tournament.Bracket.Matches.Count > 0)
            throw new Exception("Bracket already generated");

        var participants = tournament.Participants.Select(tp => tp.User).ToList();
        if (participants.Count < 2) throw new Exception("Need at least 2 participants");

        int n = 1;
        while (n < participants.Count) n *= 2;

        var rnd = new Random();
        participants = participants.OrderBy(_ => rnd.Next()).ToList();

        var bracket = tournament.Bracket ?? new Bracket { TournamentId = tournament.Id };
        if (tournament.Bracket == null) _db.Brackets.Add(bracket);

        int matchCountR1 = n / 2;
        for (int i = 0; i < matchCountR1; i++)
        {
            var p1 = i * 2 < participants.Count ? participants[i * 2] : null;
            var p2 = i * 2 + 1 < participants.Count ? participants[i * 2 + 1] : null;

            bracket.Matches.Add(new Match
            {
                Round = 1,
                Player1Id = p1?.Id,
                Player2Id = p2?.Id,
            });
        }

        int round = 2;
        int matchesInRound = matchCountR1 / 2;
        while (matchesInRound >= 1)
        {
            for (int i = 0; i < matchesInRound; i++)
            {
                bracket.Matches.Add(new Match
                {
                    Round = round,
                    Player1Id = null,
                    Player2Id = null
                });
            }

            round++;
            matchesInRound /= 2;
        }

        await _db.SaveChangesAsync();
        return bracket;
    }

    public async Task<Match> PlayAsync(int matchId, int winnerUserId)
    {
        var match = await _db.Matches
            .Include(m => m.Bracket).ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match is null) throw new Exception("Match not found");
        if (match.WinnerId != null) throw new Exception("Match already has a winner");
        if (match.Player1Id != winnerUserId && match.Player2Id != winnerUserId)
            throw new Exception("Winner must be Player1 or Player2");

        match.WinnerId = winnerUserId;

        var all = match.Bracket.Matches.OrderBy(m => m.Round).ThenBy(m => m.Id).ToList();

        var thisRoundMatches = match.Bracket.Matches
            .Where(m => m.Round == match.Round)
            .OrderBy(m => m.Id)
            .ToList();

        var idx = thisRoundMatches.FindIndex(m => m.Id == match.Id);
        if (idx < 0) throw new Exception("Internal error");

        var nextRoundMatches = match.Bracket.Matches
            .Where(m => m.Round == match.Round + 1)
            .OrderBy(m => m.Id)
            .ToList();

        if (nextRoundMatches.Count > 0)
        {
            var nextIdx = idx / 2;
            var next = nextRoundMatches[nextIdx];

            if (idx % 2 == 0)
                next.Player1Id = winnerUserId;
            else
                next.Player2Id = winnerUserId;
        }

        await _db.SaveChangesAsync();
        return match;
    }

    public Task<List<Match>> GetMatchesForRoundAsync(int bracketId, int round)
        => _db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.BracketId == bracketId && m.Round == round)
            .OrderBy(m => m.Id)
            .ToListAsync();
}
