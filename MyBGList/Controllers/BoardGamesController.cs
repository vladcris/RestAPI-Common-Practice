using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Mediator.Queries;
using MyBGList.Models;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace MyBGList.Controllers;

[Route("[controller]")]
[ApiController]
public class BoardGamesController : ControllerBase
{
    private readonly ApplicationDbContext appContext;
    private readonly ILogger<BoardGamesController> logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IMediator _mediator;

    public BoardGamesController(ApplicationDbContext appContext, 
            ILogger<BoardGamesController> logger, 
            IMemoryCache memoryCache,
            IMediator mediator) {
        this.appContext = appContext;
        this.logger = logger;
        _memoryCache = memoryCache;
        _mediator = mediator;
    }

    [HttpGet("{id:int}", Name = "GetBoardGame")]
    public async Task<ActionResult<RestDTO<BoardGame>>> GetBoardGame(int id) {
        var response = await _mediator.Send(new GetBoardGame(id));

        return Ok(response);
    }
    
    [HttpGet]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<BoardGame[]>> Get([FromQuery] RequestDTO<BoardGameDTO> input) {
        logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "Get method started at {StartTime:HH:mm}.", DateTime.Now);

        var boardGames = appContext.BoardGames.AsQueryable();

        if (!string.IsNullOrEmpty(input.FilterQuery)) {
            boardGames = boardGames.Where(bg => bg.Name.Contains(input.FilterQuery));
        }
        var recordCount = await boardGames.CountAsync();

        BoardGame[]? result = null;
        var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";
        if(_memoryCache.TryGetValue(cacheKey, out result) == false) {
            boardGames = appContext.BoardGames
                .OrderBy($"{input.SortColumn} {input.SortOrder}")
                .Skip(input.PageIndex * input.PageSize)
                .Take(input.PageSize);
            
            result = await boardGames.ToArrayAsync();
            _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
        }


        return new RestDTO<BoardGame[]> {
            Data = result!,
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

    [Authorize(Roles = RoleNames.Moderator)]
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

    [Authorize(Roles = RoleNames.Administrator)]
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

    [Authorize]
    [HttpGet("all")]
    public async Task<BoardGame[]> GetAll() {
        return await appContext.BoardGames.ToArrayAsync();
    }
}
