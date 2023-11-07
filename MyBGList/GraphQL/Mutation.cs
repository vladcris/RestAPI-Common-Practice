using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;

namespace MyBGList.GraphQL;

public class Mutation
{
    [Serial]
    [Authorize(Roles = new[] {RoleNames.Moderator})]
    public async Task<BoardGame?> UpdateBoardGame([Service] ApplicationDbContext context, BoardGameDTO model) {
        var boardGame = await context.BoardGames
                .Where(x => x.Id == model.Id)
                .FirstOrDefaultAsync();

        if(boardGame != null) {
            if(!string.IsNullOrEmpty(model.Name)) {
                boardGame.Name = model.Name;
            }

            if(model.Year.HasValue && model.Year.Value > 0) {
                boardGame.Year = model.Year.Value;
            }

            boardGame!.LastModifiedDate = DateTime.Now;
            context.BoardGames.Update(boardGame);
            await context.SaveChangesAsync(); 
        }
        return boardGame;
    }


    [Serial]
    [Authorize(Roles = new[] { RoleNames.Administrator })]
    public async Task DeleteBoardGame([Service] ApplicationDbContext context, int id) {
        var boardGame = await context.BoardGames
            .Where (x => x.Id == id)
            .FirstOrDefaultAsync();
        
        if(boardGame != null) {
            context.BoardGames.Remove(boardGame);
            await context.SaveChangesAsync();
        }
    }
}
