using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyBGList.Abstractions;
using MyBGList.Constants;
using MyBGList.GraphQL;
using MyBGList.Models;
using MyBGList.Policies;
using MyBGList.Policies.Handlers;
using MyBGList.Services;
using MyBGList.Swagger;
using MyBGList.Validators;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging
    .AddSimpleConsole()
    .AddDebug();
//.AddApplicationInsights(telemetry => 
//    telemetry.ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"], loggerOptions => { });

builder.Host.UseSerilog((context, logger) => {
    logger.ReadFrom.Configuration(context.Configuration);
    logger.Enrich.WithMachineName();
    logger.Enrich.WithThreadId();
    logger.WriteTo.File("Logs/log.txt", 
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}]" + "[{MachineName} #{ThreadId}]" + "{Message:lj} {NewLine} {Exception}",
        rollingInterval: RollingInterval.Day);
    logger.WriteTo.MSSqlServer(connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
                               sinkOptions: new MSSqlServerSinkOptions {
                                   TableName ="LogEvents",
                                   AutoCreateSqlTable = true
                               }, columnOptions: new ColumnOptions {
                                   AdditionalColumns = new SqlColumn[] {
                                       new SqlColumn {
                                           ColumnName = "SourceContext",
                                           PropertyName = "SourceContext",
                                           DataType = SqlDbType.NVarChar,
                                       },
                                       new SqlColumn {
                                             ColumnName = "MachineName",
                                             PropertyName = "MachineName",
                                             DataType = SqlDbType.NVarChar,
                                        },
                                   }
                               });
    }, writeToProviders: true);


// Add services to the container.
builder.Services.AddControllers(opt => {
    opt.ModelBindingMessageProvider.SetValueIsInvalidAccessor((value) => $"The value '{value}' is invalid.");
    opt.ModelBindingMessageProvider.SetValueMustBeANumberAccessor((value) => $"The field '{value}' must be a number.");
    opt.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, field) => $"The value '{value}' is not valid for {field}.");
    opt.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "A value is required.");

    opt.CacheProfiles.Add("NoCache", new CacheProfile { NoStore = true });
    opt.CacheProfiles.Add("Any-60", new CacheProfile { Location = ResponseCacheLocation.Any, Duration = 60 });
});

builder.Services.AddResponseCaching(opt => {
    opt.MaximumBodySize = 64 * 1024 * 1024;
    opt.SizeLimit = 100 * 1024 * 1024;
});

builder.Services.AddMemoryCache(opt => {

});
//builder.Services.AddDistributedSqlServerCache(opt => {
//    opt.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");  
//    opt.SchemaName = "dbo";
//    opt.TableName = "AppCache";
//});

builder.Services.AddStackExchangeRedisCache(opt => {
    opt.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddScoped<IValidator<RegisterDTO>, RegisterInputValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt => {
    opt.ParameterFilter<SortColumnFilter>();
    opt.ParameterFilter<SortOrderFilter>();

    var xmlPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlPath));  
    opt.EnableAnnotations();

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
        }, Array.Empty<string>() }
    });
});

var provider = builder.Configuration.GetValue("Provider", "Npgsql");
builder.Services.AddDbContext<ApplicationDbContext>(opt => {
    _ = provider switch {
        "Npgsql" => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),

        "SqlServer" => opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")),

        _ => throw new Exception($"Unsupported provider: {provider}")
    };
});




builder.Services.AddIdentity<ApiUser, IdentityRole>(opt => {
    opt.Password.RequiredLength = 8;
}).AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddAuthorization(opt => {
    opt.AddPolicy("WithDateOfBirth", policy => {
        policy.RequireClaim("DateOfBirth")
            .RequireClaim(ClaimTypes.Role, "Administrator");
    });

    opt.AddPolicy("MinAge18", policy => {
        policy.RequireAssertion(context => {
            var dateOfBirth = context.User.FindFirstValue("DateOfBirth");
            if (dateOfBirth == null) {
                return false;
            }

            if(DateTime.TryParse(dateOfBirth, out var date)) {
                var timeSpan = DateTime.Now - date;
                DateTime zeroTime = new DateTime(1, 1, 1);
                int age = (zeroTime + timeSpan).Year - 1;

                if(age >= 18) {
                    return true;
                }
            }

            return false;
        });

        opt.AddPolicy("min-age", policy => {
            policy.Requirements.Add(new MinimumAgeRequirement(18));
        });
    });
});
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    var schems = options.Schemes;
}).AddJwtBearer(options => { 
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

});


builder.Services.AddCors(opt => {
    opt.AddDefaultPolicy(policy => {
        //policy.WithOrigins(builder.Configuration["AllowedOrigins"]!);
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
    opt.AddPolicy("AnyOrigin", policy => {
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<IXkcdService, XkcdService>(opt => {
    opt.BaseAddress = new Uri("https://xkcd.com/");
});

builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections().AddFiltering().AddSorting();

//builder.Services.Configure<ApiBehaviorOptions>(opt => {
//    opt.SuppressModelStateInvalidFilter = true;
//}); 

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}else {
    app.UseHsts();
    app.Use(async (context, next) => {
        context.Response.Headers.Add("X-Frame-Options", "sameorigin");
        context.Response.Headers.Add("X-XSS-Protection", "1;mode=block");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("Content-Security-Policy", "default-src'self';");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin");

        await next();
    });
}

if(app.Configuration.GetValue<bool>("UseDeveloperExceptionPage")) {
    app.UseDeveloperExceptionPage();
} 
else {
    app.UseExceptionHandler(error => {
        error.Run(async context => {
            var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
            app.Logger.LogError(CustomLogEvents.Error_Get, exceptionHandler?.Error, 
                "An undandled error occured: Message : {error}", exceptionHandler!.Error.Message);

            var details = new ProblemDetails();
            details.Detail = exceptionHandler?.Error.Message;
            details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
            details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            details.Status = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(JsonSerializer.Serialize(details));
        }); 
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseRouting();

//app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

//app.Use((context, next) => {
//    context.Response.GetTypedHeaders().CacheControl = 
//        new Microsoft.Net.Http.Headers.CacheControlHeaderValue {
//            NoStore = true,
//            NoCache = true
//        };
//    return next();
//});

app.MapGet("/error/test", () => { throw new Exception("Exception triggered!"); });
//app.MapGet("/", () => "Hello World!");

app.MapControllers();

app.Run();


namespace MyBGList
{
    public partial class Program { }
}