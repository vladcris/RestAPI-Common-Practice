using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyBGList.Models;
using System.Linq;

namespace MyBGList.Controllers;
[Route("account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApiUser> _userManager;
    private readonly SignInManager<ApiUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<RegisterDTO> _validator;

    public AccountController(ILogger<AccountController> logger,
            ApplicationDbContext context,
            UserManager<ApiUser> userManager,
            SignInManager<ApiUser> signInManager,
            IConfiguration configuration,
            IValidator<RegisterDTO> validator)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _validator = validator;
    }

    [HttpPost("register")]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<ActionResult> Register([FromBody] RegisterDTO input) {
        ValidationResult validationResult = _validator.Validate(input);
        if (!validationResult.IsValid) {
            return UnprocessableEntity(validationResult.Errors.
                ToDictionary(k => k.PropertyName, v => v.ErrorMessage));
        }

        var user = new ApiUser {
            UserName = input.UserName,
            Email = input.Email
        };

        var result = await _userManager.CreateAsync(user, input.Password!);
        if (!result.Succeeded) {
            return UnprocessableEntity(result.Errors.ToDictionary(k => k.Code, v => v.Description));
        }

        _logger.LogInformation("User {user} with {email} has been created", user.UserName, user.Email);
        return StatusCode(201, $"User '{user.UserName}' has been created.");
    }

    [HttpPost("login")]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<ActionResult> Login() {
        throw new NotImplementedException();
    }


    private UnprocessableEntityObjectResult UnprocessableEntity(IDictionary<string, string> errors) {
        foreach (var error in errors) {
            ModelState.AddModelError(error.Key, error.Value);
        }

        var details = new ValidationProblemDetails(ModelState);
        details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        details.Status = StatusCodes.Status422UnprocessableEntity;

        return UnprocessableEntity(details);
    }
}
