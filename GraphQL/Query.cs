using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentApi.Data;
using TournamentApi.Domain;
using TournamentApi.Services;

namespace TournamentApi.GraphQL;

public class Query
{
    [UseProjection]
    public IQueryable<Tournament> Tournaments([Service] AppDbContext db)
        => db.Tournaments;

    [UseProjection]
    public IQueryable<Match> Matches([Service] AppDbContext db)
        => db.Matches;

    [Authorize]
    public async Task<List<Match>> MyMatches([Service] AppDbContext db, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        return await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1Id == userId || m.Player2Id == userId)
            .OrderByDescending(m => m.Id)
            .ToListAsync();
    }

    [Authorize]
    [GraphQLName("getMatchesForRound")]
    public async Task<List<Match>> GetMatchesForRound(
        [Service] AppDbContext db,
        [Service] BracketService bracketService,
        int tournamentId,
        int round)
    {
        var bracket = await db.Brackets.FirstOrDefaultAsync(b => b.TournamentId == tournamentId);
        if (bracket is null) throw new Exception("Bracket not found");
        return await bracketService.GetMatchesForRoundAsync(bracket.Id, round);
    }
}
