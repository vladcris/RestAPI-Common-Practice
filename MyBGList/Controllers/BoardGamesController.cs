using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class BoardGamesController : ControllerBase
{
    private readonly ApplicationDbContext appContext;

    public BoardGamesController(ApplicationDbContext appContext)
    {
        this.appContext = appContext;
    }
    [HttpGet]
    public async Task<RestDTO<BoardGame[]>> Get(int pageIndex = 0, 
        int pageSize = 10, 
        string? sortColumn = "Name", 
        string? sortOrder = "ASC", 
        string? filterQuery = null) {

        var boardGames = appContext.BoardGames.AsQueryable();
        if (!string.IsNullOrEmpty(filterQuery)) {
            boardGames = boardGames.Where(bg => bg.Name.Contains(filterQuery));
        }
        var recordCount = await boardGames.CountAsync();

        boardGames = appContext.BoardGames
            .OrderBy($"{sortColumn} {sortOrder}")
            .Skip(pageIndex * pageSize)
            .Take(pageSize);

        return new RestDTO<BoardGame[]> {
            Data = await boardGames.ToArrayAsync(),
            PageIndex = pageIndex,
            PageSize = pageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames", new {pageIndex, pageSize}, Request.Scheme)!,
                    "self",
                    "GET")
            }
        };
    }


    [HttpPost(Name = "UpdateBoardGame")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<BoardGame?>> Post([FromBody] BoardGameDTO model) {
        var boardGame = await appContext.BoardGames.FindAsync(model.Id);
        if (boardGame != null) {
            if (!string.IsNullOrEmpty(model.Name)) {
                boardGame.Name = model.Name;
            }
            if (model.Year.HasValue && model.Year > 0) {
                boardGame.Year = model.Year.Value;
            }
            boardGame.LastModifiedDate = DateTime.Now;

            appContext.BoardGames.Update(boardGame);
            await appContext.SaveChangesAsync();
        }


        return new RestDTO<BoardGame?> {
            Data = boardGame,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames", model, Request.Scheme)!,
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
