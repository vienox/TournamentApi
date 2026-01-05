using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentApi.Auth;
using TournamentApi.Data;
using TournamentApi.Domain;
using TournamentApi.Services;

namespace TournamentApi.GraphQL;

public record AuthPayload(string Token, User User);

public class Mutation
{
    public async Task<AuthPayload> Register(
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt,
        string firstName,
        string lastName,
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new Exception("Email already used");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwt.CreateToken(user);
        return new AuthPayload(token, user);
    }

    public async Task<AuthPayload> Login(
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt,
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) throw new Exception("Invalid credentials");

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var token = jwt.CreateToken(user);
        return new AuthPayload(token, user);
    }

    [Authorize]
    public async Task<Tournament> CreateTournament(
        [Service] AppDbContext db,
        string name,
        DateTime startDate)
    {
        var t = new Tournament { Name = name, StartDate = startDate, Status = "DRAFT" };
        db.Tournaments.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    [Authorize]
    public async Task<Tournament> AddParticipant(
        [Service] AppDbContext db,
        ClaimsPrincipal principal,
        int tournamentId)
    {
        var userId = int.Parse(principal.FindFirstValue("sub")!);

        var t = await db.Tournaments
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new Exception("Tournament not found");
        if (t.Status != "DRAFT") throw new Exception("Cannot join after start");

        var exists = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!exists)
            db.TournamentParticipants.Add(new TournamentParticipant { TournamentId = tournamentId, UserId = userId });

        await db.SaveChangesAsync();
        return t;
    }

    [Authorize]
    public async Task<Tournament> StartTournament([Service] AppDbContext db, int tournamentId)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId);
        if (t is null) throw new Exception("Tournament not found");

        t.Status = "STARTED";
        await db.SaveChangesAsync();
        return t;
    }

    [Authorize]
    public async Task<Tournament> FinishTournament([Service] AppDbContext db, int tournamentId)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId);
        if (t is null) throw new Exception("Tournament not found");

        t.Status = "FINISHED";
        await db.SaveChangesAsync();
        return t;
    }

    [Authorize]
    public Task<Bracket> GenerateBracket(
        [Service] BracketService bracketService,
        int tournamentId)
        => bracketService.GenerateBracketAsync(tournamentId);

    [Authorize]
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

    [Authorize]
    public Task<Match> PlayMatch(
        [Service] BracketService bracketService,
        int matchId,
        int winnerUserId)
        => bracketService.PlayAsync(matchId, winnerUserId);
}
