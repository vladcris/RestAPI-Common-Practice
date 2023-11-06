using MediatR;
using MyBGList.DTO;
using MyBGList.Models;

namespace MyBGList.Mediator.Queries;

public class GetDomains : IRequest<RestDTO<Domain[]>>
{
    public RequestDTO<DomainDTO> Domain { get; set; }
    public GetDomains(RequestDTO<DomainDTO> domain)
    {
        Domain = domain;
    }
}
