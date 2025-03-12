using Ingos.ServiceForFeatureFlags.Server;
using Ingos.ServiceForFeatureFlags.Server.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization();
// Add services to the container.
builder.Configuration.AddJsonFile("appsettings.json", true, true);
builder.Services.AddCors(options => 
{
    options.AddPolicy("AllowFrontend", policy => 
    {
        policy.WithOrigins("https://127.0.0.1:50357")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Разрешить передачу аутентификации
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PostgreSqlService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");


app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();