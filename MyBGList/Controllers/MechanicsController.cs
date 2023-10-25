using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers;
[Route("[controller]")]
[ApiController]
public class MechanicsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public MechanicsController(ApplicationDbContext dbContext) {
        this.dbContext = dbContext;
    }


    [HttpGet(Name = "GetMechanics")]
    public async Task<RestDTO<Mechanic[]>> Get([FromQuery] RequestDTO<MechanicDTO> input) {
        var mechanics = dbContext.Mechanics.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery)) {
            mechanics = mechanics.Where(bg => bg.Name.Contains(input.FilterQuery));
        }
        var recordCount = await mechanics.CountAsync();

        mechanics = dbContext.Mechanics
            .OrderBy($"{input.SortColumn} {input.SortOrder}")
            .Skip(input.PageIndex * input.PageSize)
            .Take(input.PageSize);

        return new RestDTO<Mechanic[]> {
            Data = await mechanics.ToArrayAsync(),
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
