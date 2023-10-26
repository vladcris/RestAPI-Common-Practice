using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Attributes;
using MyBGList.DTO;
using MyBGList.Models;
using System;
using System.Linq.Dynamic.Core;
using System.Net;

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
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    [ManualValidationFilter]
    public async Task<ActionResult<RestDTO<Domain[]?>>> Get([FromQuery] RequestDTO<DomainDTO> input) {
        if(!ModelState.IsValid) {
            var details = new ValidationProblemDetails(ModelState);
            details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if(ModelState.Keys.Any(k => k == "PageSize")) {

                details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2";
                details.Status = StatusCodes.Status501NotImplemented;
                return new ObjectResult(details) {
                    StatusCode = StatusCodes.Status501NotImplemented
                };
            }else {
                details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                details.Status = StatusCodes.Status400BadRequest;
                return new BadRequestObjectResult(details);
            }
        }

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
