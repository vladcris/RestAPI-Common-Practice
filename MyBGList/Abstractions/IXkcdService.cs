using MyBGList.Models.Xkcd;

namespace MyBGList.Abstractions;

public interface IXkcdService
{
    Task<Xkcd?> GetPostAsync(int id);

    Task<Xkcd?[]> GetPostsAsync(int[] ids);

    IAsyncEnumerable<Xkcd?> GetPostsIAsync(int[] ids);
}
