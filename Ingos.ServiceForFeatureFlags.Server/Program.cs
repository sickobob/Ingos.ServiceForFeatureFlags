using Ingos.ServiceForFeatureFlags.Server;
using Ingos.ServiceForFeatureFlags.Server.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Configuration.AddJsonFile("appsettings.json", true, true);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PostgreSqlService>();
builder.Services.AddCors(options => options.AddPolicy("AllowAll",
    builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("AllowAll");
app.MapControllers();
app.MapFallbackToFile("/index.html");
app.Run();