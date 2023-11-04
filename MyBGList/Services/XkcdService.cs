using MyBGList.Abstractions;
using MyBGList.Models.Xkcd;
using RestSharp;
using System.Threading.Tasks;

namespace MyBGList.Services;

public class XkcdService : IXkcdService
{
    private readonly RestClient _client;
    public XkcdService(HttpClient client)
    {
        _client = new RestClient(client);
    }
    public async Task<Xkcd?> GetPostAsync(int id) {
        var request = new RestRequest($"{id}/info.0.json");

        var response = await _client.GetAsync<Xkcd>(request);

        return response;
    }

    public async Task<Xkcd?[]> GetPostsAsync(int[] ids) {
        var tasks = new List<Task<Xkcd?>>();
        foreach(var id in ids) { 
            tasks.Add(GetPostAsync(id));
        }

        tasks.Add(GetPostAsync(1000000));

        var task = await Task.WhenAll(tasks);

        return task;    
    }

    public async IAsyncEnumerable<Xkcd?> GetPostsIAsync(int[] ids) {
        var tasks = new List<Task<Xkcd?>>();
        var count = ids.Length;
        foreach (var id in ids) {
            tasks.Add(GetPostAsync(id));
        }

        tasks.Add(GetPostAsync(1000000));

        while (tasks.Any() && count-- > 0) {
            var executedTask = await Task.WhenAny(tasks);
            tasks.Remove(executedTask);

            var post = await executedTask;
            yield return post;
        }
    }
}
