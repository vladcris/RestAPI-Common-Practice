using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class BoardGamesController : ControllerBase
{
    private readonly ApplicationDbContext appContext;
    private readonly ILogger<BoardGamesController> logger;

    public BoardGamesController(ApplicationDbContext appContext, ILogger<BoardGamesController> logger)
    {
        this.appContext = appContext;
        this.logger = logger;
    }
    [HttpGet]
    public async Task<RestDTO<BoardGame[]>> Get([FromQuery] RequestDTO<BoardGameDTO> input) {
        logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "Get BoardGames");
        
        var boardGames = appContext.BoardGames.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery)) {
            boardGames = boardGames.Where(bg => bg.Name.Contains(input.FilterQuery));
        }
        var recordCount = await boardGames.CountAsync();

        boardGames = appContext.BoardGames
            .OrderBy($"{input.SortColumn} {input.SortOrder}")
            .Skip(input.PageIndex * input.PageSize)
            .Take(input.PageSize);

        return new RestDTO<BoardGame[]> {
            Data = await boardGames.ToArrayAsync(),
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames", new {input.PageIndex, input.PageSize}, Request.Scheme)!,
                    "self",
                    "GET")
            }
        };
    }


    [HttpPost(Name = "UpdateBoardGame")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<BoardGame?>> Post([FromBody] BoardGameDTO input) {
        var boardGame = await appContext.BoardGames.FindAsync(input.Id);
        if (boardGame != null) {
            if (!string.IsNullOrEmpty(input.Name)) {
                boardGame.Name = input.Name;
            }
            if (input.Year.HasValue && input.Year > 0) {
                boardGame.Year = input.Year.Value;
            }
            boardGame.LastModifiedDate = DateTime.Now;

            appContext.BoardGames.Update(boardGame);
            await appContext.SaveChangesAsync();
        }


        return new RestDTO<BoardGame?> {
            Data = boardGame,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames", input, Request.Scheme)!,
                    "self",
                    "POST")
            }
        };
    }

    [HttpDelete("{id}", Name = "DeleteBoardGame")]
    public async Task<RestDTO<BoardGame?>> Delete(int id) {
        var boardGame = await appContext.BoardGames.Where(bg => bg.Id == id).FirstOrDefaultAsync();

        if(boardGame != null) {
            appContext.BoardGames.Remove(boardGame);
            await appContext.SaveChangesAsync();
        }

        return new RestDTO<BoardGame?> {
            Data = boardGame,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames", new {id}, Request.Scheme)!,
                    "self",
                    "DELETE")
            }
        };
    }


    [HttpGet("all")]
    public async Task<BoardGame[]> GetAll() {
        return await appContext.BoardGames.ToArrayAsync();
    }
}
