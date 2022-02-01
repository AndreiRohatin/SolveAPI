
using Microsoft.AspNetCore.Authentication.Cookies;
using SolveAPI.Models;

var builder = WebApplication.CreateBuilder(args);
var AllowSpecificOrigins = "_AllowSpecificOrigins";

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//create auth system without entity framework
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
builder.Services.AddHttpContextAccessor();

//Enable CORS (Cross-Origin-Resource-Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
            .AllowAnyOrigin()
            .AllowAnyHeader();
        });
});

//set authentication options
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = CookieAuthenticationDefaults.AuthenticationScheme;
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Forbidden/";
    });


//initialize Database
DatabaseManager.Start();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//initialize cookie usage and authentication
app.UseAuthentication();
app.UseAuthorization();


app.UseCors(AllowSpecificOrigins);


app.MapControllers();

app.Run();
