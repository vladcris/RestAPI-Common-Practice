using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyBGList.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

    /// <summary>
    /// What it does
    /// </summary>
    /// <param name="input">Some info</param>
    /// <returns>What returns</returns>
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
    public async Task<ActionResult> Login([FromBody] LoginDTO input) {
        var issuer = _configuration.GetSection("Jwt:Issuer").Value;
        var audience = _configuration.GetSection("Jwt:Audience").Value;
        var key = _configuration.GetSection("Jwt:Key").Value;

        var user = await _userManager.FindByNameAsync(input.UserName!);
        if(user == null) {
            return StatusCode(401, "Invalid credentials");
        }

        var login = await _signInManager.PasswordSignInAsync(user: user,
            password: input.Password!,
            isPersistent: false,
            lockoutOnFailure: false);


        //var login = await _userManager.CheckPasswordAsync(user, input.Password!);

        if (!login.Succeeded) {
            return StatusCode(401, "Invalid credentials");
        }

        var roles = await _userManager.GetRolesAsync(user: user);
        var existingClaims = await _userManager.GetClaimsAsync(user: user); 

        var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.UserName!) };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        claims.AddRange(existingClaims);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
        var signInCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken jwt = new JwtSecurityToken(issuer: issuer,
                                                    audience: audience,
                                                    claims: claims,
                                                    expires: DateTime.Now.AddMinutes(30),
                                                    signingCredentials: signInCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return Ok(new { Access_Token = token });
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
