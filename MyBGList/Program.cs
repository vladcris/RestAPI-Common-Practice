using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;
using MyBGList.Swagger;

var builder = WebApplication.CreateBuilder(args);

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
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthorization();

app.MapGet("/error", (HttpContext context) => {
    var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
    var details = new ProblemDetails();
    details.Detail = exceptionHandler?.Error.Message;
    details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier; 
    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
    details.Status = StatusCodes.Status500InternalServerError;
    Results.Problem(details);
    });
app.MapGet("/error/test", () => { throw new Exception("Exception triggered!"); });

app.MapControllers();

app.Run();
