using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers;
[Route("[controller]")]
[ApiController]
public class DomainsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public DomainsController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }


    [HttpGet(Name = "GetDomains")]
    public async Task<RestDTO<Domain[]>> Get([FromQuery] RequestDTO<DomainDTO> input) {
        var domains = dbContext.Domains.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery)) {
            domains = domains.Where(bg => bg.Name.Contains(input.FilterQuery));
        }
        var recordCount = await domains.CountAsync();

        domains = dbContext.Domains
            .OrderBy($"{input.SortColumn} {input.SortOrder}")
            .Skip(input.PageIndex * input.PageSize)
            .Take(input.PageSize);

        return new RestDTO<Domain[]> {
            Data = await domains.ToArrayAsync(),
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Domains", new {input.PageIndex, input.PageSize}, Request.Scheme)!,
                    "self",
                    "GET")
            }
        };
    }

    [HttpPost(Name = "UpdateDomains")]
    [ResponseCache(NoStore = true)]
    public async Task<RestDTO<Domain?>> Post([FromBody] DomainDTO model) {
        var domain = await dbContext.Domains.FindAsync(model.Id);
        if (domain != null) {
            if (!string.IsNullOrEmpty(model.Name)) {
                domain.Name = model.Name;
            }

            domain.LastModifiedDate = DateTime.Now;

            dbContext.Domains.Update(domain);
            await dbContext.SaveChangesAsync();
        }


        return new RestDTO<Domain?> {
            Data = domain,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Domains", model, Request.Scheme)!,
                    "self",
                    "POST")
            }
        };
    }

    [HttpDelete("{id}", Name = "DeleteDomain")]
    public async Task<RestDTO<Domain?>> Delete(int id) {
        var domain = await dbContext.Domains.Where(bg => bg.Id == id).FirstOrDefaultAsync();

        if (domain != null) {
            dbContext.Domains.Remove(domain);
            await dbContext.SaveChangesAsync();
        }

        return new RestDTO<Domain?> {
            Data = domain,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "Domains", new {id}, Request.Scheme)!,
                    "self",
                    "DELETE")
            }
        };
    }
}
