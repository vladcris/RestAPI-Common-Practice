using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using MyBGList.DTO;
using MyBGList.Mediator.Queries;
using MyBGList.Models;

namespace MyBGList.Mediator.QueryHandlers;

public class GetDomainsHandler : IRequestHandler<GetDomains, RestDTO<Domain[]>>
{
    private readonly ILogger<GetDomainsHandler> _logger;
    private readonly ApplicationDbContext _context;
    private readonly LinkGenerator _linkGenerator;

    public GetDomainsHandler(ILogger<GetDomainsHandler> logger, ApplicationDbContext context, LinkGenerator linkGenerator)
    {
        _logger = logger;
        _context = context;
        _linkGenerator = linkGenerator;
    }
    public async Task<RestDTO<Domain[]>> Handle(GetDomains request, CancellationToken cancellationToken) {
        var input = request.Domain;

        var domains = _context.Domains.AsQueryable();
        if (!string.IsNullOrEmpty(input.FilterQuery)) {
            domains = domains.Where(bg => bg.Name.Contains(input.FilterQuery));
        }
        var recordCount = await domains.CountAsync();

        domains = _context.Domains
            .OrderBy($"{input.SortColumn} {input.SortOrder}")
            .Skip(input.PageIndex * input.PageSize)
            .Take(input.PageSize);

        var result = await domains.ToArrayAsync();

        return new RestDTO<Domain[]> {
            Data = result,
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO> {
                new LinkDTO(
                    _linkGenerator.GetUriByAction(action: "GetMediator",
                        controller: "domains",
                        values: new {input.PageIndex, input.PageSize},
                        scheme: "https",
                        host: new HostString("localhost"))!,
                    "self",
                    "GET")
            }
        };
    }
}

