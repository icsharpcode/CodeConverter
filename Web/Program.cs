using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Code Converter API", Version = "v1" });
});

builder.Services.AddEndpointsApiExplorer();

string localOrigins = "local";
builder.Services.AddCors(options => {
    options.AddPolicy(name: localOrigins, b =>
        b.WithOrigins("https://localhost:44463").AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment()) {
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseCors(localOrigins);

app.MapPost("/api/convert", async (ConvertRequest todo) => await WebConverter.ConvertAsync(todo))
    .RequireCors(localOrigins)
    .Produces<ConvertResponse>();

app.MapGet("/api/version", () => CodeConverterVersion.GetVersion())
    .RequireCors(localOrigins)
    .Produces<string>();

app.MapFallbackToFile("index.html");

app.Run();