using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.Abstractions;
using MyBGList.Models.Xkcd;
using RestSharp;

namespace MyBGList.Controllers;
[Route("apis")]
[ApiController]
public class MultipleAPICallsController : ControllerBase
{
    private readonly ILogger<MultipleAPICallsController> _logger;
    private readonly IXkcdService _xkcdService;

    public MultipleAPICallsController(ILogger<MultipleAPICallsController> logger, IXkcdService xkcdService) {
        _logger = logger;
        _xkcdService = xkcdService;
    }

    [HttpGet("in-sequance/{count:int}")]
    public async Task<IActionResult> GetInSequence(int count) {
        var rand = new Random();
        var response = new List<Xkcd>();
        for (int i = 0; i < count; i++) {
            int id = rand.Next(1000, 2000);

            var post = await _xkcdService.GetPostAsync(id);
            response.Add(post!);
        }

        return Ok(response);
    }

    [HttpGet("in-parallel/{count:int}")]
    public async Task<IActionResult> GetInParallel(int count) {
        var rand = new Random();
        var ids = Enumerable.Range(0, count).Select(_ => rand.Next(1000, 2000));

        var posts = await _xkcdService.GetPostsAsync(ids.ToArray());

        return Ok(posts);
    }

    [HttpGet("async-enumerable/{count:int}")]
    public async IAsyncEnumerable<Xkcd?> GetAsyncEnumerable(int count) {
        var rand = new Random();
        var ids = Enumerable.Range(0, count).Select(_ => rand.Next(1000, 2000));

        //return _xkcdService.GetPostsIAsync(ids.ToArray());

        await foreach (var post in _xkcdService.GetPostsIAsync(ids.ToArray())) {
            yield return post;
        }
    }
}
