using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

//.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500));
//app.MapGet("/error/test", () => { throw new Exception("Exception triggered!"); });

app.MapControllers();

app.Run();
