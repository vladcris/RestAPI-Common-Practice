using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using MyBGList.Models.Csv;
using System.Globalization;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IWebHostEnvironment webHost;

    public SeedController(ApplicationDbContext dbContext, IWebHostEnvironment webHost)
    {
        this.dbContext = dbContext;
        this.webHost = webHost;
    }


    [HttpPut(Name = "Seed")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Put() {
        var csvConfig = new CsvConfiguration(CultureInfo.GetCultureInfo("pt-BR")) {
            HasHeaderRecord = true,
            Delimiter = ";"
        };

        using var reader = new StreamReader(Path.Combine(webHost.ContentRootPath, "Data/bgg_dataset.csv"));
        using var csv = new CsvReader(reader, csvConfig);

        var existingBoardGames = await dbContext.BoardGames.ToDictionaryAsync(key => key.Id);

        var existingsDomains = await dbContext.Domains.ToDictionaryAsync(key => key.Name);
     
        var existingsMechanics = await dbContext.Mechanics.ToDictionaryAsync(key => key.Name);
        var now = DateTime.Now;


        var records = csv.GetRecords<BggRecord>();
        var skippedRows = 0;

        foreach (var record in records) {

            if (!record.ID.HasValue ||
                string.IsNullOrEmpty(record.Name) ||
                existingBoardGames.ContainsKey(record.ID.Value)
                ) {
                skippedRows++;
                continue;
            }

            var boardGame = new BoardGame {
                Id = record.ID.Value,
                Name = record.Name!,
                Year = record.YearPublished ?? 0,
                MinPlayers = record.MinPlayers ?? 0,
                MaxPlayers = record.MaxPlayers ?? 0,
                PlayTime = record.PlayTime ?? 0,
                MinAge = record.MinAge ?? 0,
                UserRated = record.UsersRated ?? 0,
                RatingAverage = record.RatingAverage ?? 0,
                BGGRank = record.BGGRank ?? 0,
                ComplexityAverage = record.ComplexityAverage ?? 0,
                OwnerUsers = record.OwnedUsers ?? 0,
                CreatedDate = now,
                LastModifiedDate = now
            };

            dbContext.BoardGames.Add(boardGame);
            existingBoardGames.Add(boardGame.Id, boardGame);

            if(!string.IsNullOrEmpty(record.Domains)) {
                var domainNames = record.Domains
                    .Split(',', StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase);

                foreach (var domainName in domainNames) {

                    if(!existingsDomains.TryGetValue(domainName, out var domain)) {
                        domain = new Domain {
                            Name = domainName,
                            CreatedDate = now,
                            LastModifiedDate = now
                        };

                        dbContext.Domains.Add(domain);
                        existingsDomains.Add(domainName, domain);
                    }

                    dbContext.BoardGames_Domains.Add(new BoardGames_Domains {
                        BoardGame = boardGame,
                        Domain = domain,
                        CreatedDate = now
                    });
                }
            }

            if (!string.IsNullOrEmpty(record.Mechanics)) {
                var mechanicNames = record.Mechanics
                    .Split(',', StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase);

                foreach (var mechanicName in mechanicNames) {

                    if (!existingsMechanics.TryGetValue(mechanicName, out var mechanic)) {
                        mechanic = new Mechanic {
                            Name = mechanicName,
                            CreatedDate = now,
                            LastModifiedDate = now
                        };

                        dbContext.Mechanics.Add(mechanic);
                        existingsMechanics.Add(mechanicName, mechanic);
                    }

                    dbContext.BoardGames_Mechanics.Add(new BoardGames_Mechanics {
                        BoardGame = boardGame,
                        Mechanic = mechanic,
                        CreatedDate = now
                    });
                }
            }


        }

        using var transaction = await dbContext.Database.BeginTransactionAsync();

        dbContext.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT BoardGames ON");

        await dbContext.SaveChangesAsync();

        dbContext.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT BoardGames OFF");

        transaction.Commit();


        return new JsonResult(new {
            BoardGames = dbContext.BoardGames.Count(),
            Domains = dbContext.Domains.Count(),
            Mechanics = dbContext.Mechanics.Count(),
            SkippedRows = skippedRows
        });
    }
}
