using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.Models;
using MyBGList.Swagger;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.Text.Json;
using System.Threading;

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
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt => {
    opt.ParameterFilter<SortColumnFilter>();
    opt.ParameterFilter<SortOrderFilter>();
});

builder.Services.AddDbContext<ApplicationDbContext>(opt => {
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
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

//builder.Services.Configure<ApiBehaviorOptions>(opt => {
//    opt.SuppressModelStateInvalidFilter = true;
//}); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
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

app.UseAuthorization();

app.MapGet("/error/test", () => { throw new Exception("Exception triggered!"); });
app.MapGet("/", () => "Hello World!");

app.MapControllers();

app.Run();


namespace MyBGList
{
    public partial class Program { }
}