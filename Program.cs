using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using upload_architecture.Data;
using upload_architecture.Services;
using upload_architecture.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<AllowedFileTypesService>();
builder.Services.AddSingleton<ClamAvService>();

builder.Services.AddHostedService<FileScanWorker>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Upload Architecture API",
            Version = "v1",
            Description = "API para gerar URLs assinadas de upload e confirmar arquivos enviados ao Blob Storage."
        };

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Upload Architecture API");
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.MapControllers();

app.Run();
