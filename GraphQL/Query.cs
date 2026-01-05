using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentApi.Data;
using TournamentApi.Domain;

namespace TournamentApi.GraphQL;

public class Query
{
    public IQueryable<Tournament> Tournaments([Service] AppDbContext db)
        => db.Tournaments.AsNoTracking();

    public IQueryable<Match> Matches([Service] AppDbContext db)
        => db.Matches.AsNoTracking();

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
}
