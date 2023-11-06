using MediatR;
using MyBGList.DTO;
using MyBGList.Models;

namespace MyBGList.Mediator.Queries;

public class GetBoardGame : IRequest<RestDTO<BoardGame>>
{
    public int Id { get; set; }
    public GetBoardGame(int id)
    {
        Id = id;
    }
}
