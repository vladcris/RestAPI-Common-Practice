using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Extensions;
using MyBGList.Models;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace MyBGList.Controllers;
[Route("[controller]")]
[ApiController]
public class MechanicsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IDistributedCache _distributedCache;

    public MechanicsController(ApplicationDbContext dbContext, IDistributedCache distributedCache) {
        this.dbContext = dbContext;
        _distributedCache = distributedCache;
    }


    [HttpGet(Name = "GetMechanics")]
    public async Task<ActionResult<RestDTO<Mechanic[]>>> Get([FromQuery] RequestDTO<MechanicDTO> input) {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(3000);
        var token = cancellationTokenSource.Token;
        //if query take longer than 1 second, cancel it
        // delay make sure it always take longer than 2 seconds
        var delayTask = Task.Delay(2000, token);

        while (true) {
            var mechanics = dbContext.Mechanics.AsQueryable();
            if (!string.IsNullOrEmpty(input.FilterQuery)) {
                mechanics = mechanics.Where(bg => bg.Name.Contains(input.FilterQuery));
            }
            var recordCount = await mechanics.CountAsync(token);

            Mechanic[]? result = null;
            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";
            if (_distributedCache.TryGetValue(cacheKey, out result) == false) {
                mechanics = dbContext.Mechanics
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Skip(input.PageIndex * input.PageSize)
                    .Take(input.PageSize);
                result = await mechanics.ToArrayAsync(token);

                _distributedCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }

            await delayTask;

            return new RestDTO<Mechanic[]> {
                Data = result!,
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Mechanics", new {input.PageIndex, input.PageSize}, Request.Scheme)!,
                    "self",
                    "GET")
            }
            };
        }
    }


    [Authorize(Roles = RoleNames.Moderator)]
    [HttpPost(Name = "UpdateMechanics")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<Mechanic?>> Post([FromBody] MechanicDTO model) {
        var mechanic = await dbContext.Mechanics.FindAsync(model.Id);
        if (mechanic != null) {
            if (!string.IsNullOrEmpty(model.Name)) {
                mechanic.Name = model.Name;
            }

            mechanic.LastModifiedDate = DateTime.Now;

            dbContext.Mechanics.Update(mechanic);
            await dbContext.SaveChangesAsync();
        }


        return new RestDTO<Mechanic?> {
            Data = mechanic,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Mechanics", model, Request.Scheme)!,
                    "self",
                    "POST")
            }
        };
    }


    [Authorize(Roles = RoleNames.Administrator)]
    [HttpDelete("{id}", Name = "DeleteMechanic")]
    public async Task<RestDTO<Mechanic?>> Delete(int id) {
        var mechanic = await dbContext.Mechanics.Where(bg => bg.Id == id).FirstOrDefaultAsync();

        if (mechanic != null) {
            dbContext.Mechanics.Remove(mechanic);
            await dbContext.SaveChangesAsync();
        }

        return new RestDTO<Mechanic?> {
            Data = mechanic,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Mechanics", new {id}, Request.Scheme)!,
                    "self",
                    "DELETE")
            }
        };
    }
}
