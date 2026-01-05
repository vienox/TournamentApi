using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TournamentApi.Auth;
using TournamentApi.Data;
using TournamentApi.GraphQL;
using TournamentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(opt =>
{
    opt.Issuer = "TournamentApi";
    opt.Audience = "TournamentApi";
    opt.Key = "secretkey123secretkey123secretkey123";
    opt.ExpMinutes = 120;
});

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite("Data Source=tournament.db");
});

builder.Services.AddScoped<BracketService>();

var jwtOpt = new JwtOptions
{
    Issuer = "TournamentApi",
    Audience = "TournamentApi",
    Key = "secretkey123secretkey123secretkey123",
    ExpMinutes = 120
};
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidAudience = jwtOpt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Key)),
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");
app.Run();
