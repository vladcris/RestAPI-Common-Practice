using MediatR;
using MyBGList.DTO;
using MyBGList.Mediator.Queries;
using MyBGList.Models;

namespace MyBGList.Mediator.QueryHandlers;

public class GetBoardGameHandler : IRequestHandler<GetBoardGame, RestDTO<BoardGame>>
{
    private readonly ApplicationDbContext _context;
    private readonly LinkGenerator _linkGenerator;

    public GetBoardGameHandler(ApplicationDbContext context, LinkGenerator linkGenerator)
    {
        _context = context;
        _linkGenerator = linkGenerator;
    }
    public async Task<RestDTO<BoardGame>> Handle(GetBoardGame request, CancellationToken cancellationToken) {
        var boardGame = await _context.BoardGames.FindAsync(request.Id, cancellationToken);

        if (boardGame == null) {
            return default!;
        }

        var response = new RestDTO<BoardGame> {
            Data = boardGame,
            Links = new List<LinkDTO> { 
                new LinkDTO (
                    href: _linkGenerator.GetUriByAction(action: "GetBoardGame",
                        controller: "boardgames",
                        values: new{Id = request.Id},
                        scheme: "https",
                        host: new HostString("localhost"))!,
                    rel: "self",
                    type: "GET"
                )
            }
        };

        return response;
    }
}
