using Azure.Storage.Queues;
using HngStage5Minimal;
using HngStage5Minimal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Hng") ?? "Data Source=Hng.db";
var baseUrl = builder.Configuration.GetValue<string>("base_url");
var root = builder.Configuration.GetValue<string>("root");
builder.Services.AddSqlite<HngDb>(connectionString);
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = builder.Environment.ApplicationName,
        Version = "v1"
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json",
                                $"{builder.Environment.ApplicationName} v1"));
// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
IMemoryCache cache;
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HngDb>();
    db.Database.Migrate();
    cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
}

app.MapPost("/api/create", async (HngDb db, FileDetails file) =>
{
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not provided"
        });
    }
    var fileDetails = await db.Files
    .FirstOrDefaultAsync(x => x.Filename.Contains(Path.GetFileNameWithoutExtension(file.Filename)));

    if (fileDetails != null)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = "File already exists"
        });
    }
    FileDetails newFile = new()
    {
        Filename = file.Filename
    };
    await db.Files.AddAsync(newFile);
    await db.SaveChangesAsync();
    return Results.Created($"/api/{newFile.Id}", new
    {
        Success = true,
        Message = "File created",
        Data = newFile
    });
});

app.MapPost("/api/{id}/upload", async (HngDb db, HttpRequest request, int id) =>
{
    var file = await db.Files.FirstOrDefaultAsync(x => x.Id == id);
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    IMemoryCache _cache;

    var stream = new StreamReader(request.Body);
    byte[] data = Convert.FromBase64String(await stream.ReadToEndAsync());
    byte[] bytes;
    byte[] existing;

    try
    {
        existing = cache.Get<byte[]>(file.Filename);
        if (existing == null) throw new ArgumentNullException();
        bytes = new byte[existing.Length + data.Length];
        Buffer.BlockCopy(existing, 0, bytes, 0, existing.Length);
        Buffer.BlockCopy(data, 0, bytes, existing.Length, data.Length);
    }
    catch (Exception)
    {
        bytes = new byte[data.Length];
        Buffer.BlockCopy(data, 0, bytes, 0, data.Length);
    }

    cache.Set(file.Filename, bytes, TimeSpan.FromHours(2));

    return Results.Ok(new
    {
        Success = true,
        Message = "File uploaded"
    });
});

app.MapGet("/api/{id}/complete", async (HngDb db, HttpRequest request, int id) =>
{
    var file = await db.Files.FirstOrDefaultAsync(x => x.Id == id);
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }
    File.WriteAllBytes($@"{root}/videos/{file.Filename}", cache.Get<byte[]>(file.Filename));
    cache.Remove(file.Filename);
    string queueName = "hngstage5";
    string connectionString = builder.Configuration.GetConnectionString("StorageConnectionString");

    QueueClient queueClient = new(connectionString, queueName);
    await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(file.Filename)));
    return Results.Ok(new
    {
        Success = true,
        Message = "File completed"
    });
});

app.MapGet("/api/{id}", async (HngDb db, HttpRequest request, int id) =>
{
    var fileDetails = await db.Files.FirstOrDefaultAsync(x => x.Id == id);
    if (fileDetails == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var file = Directory.GetFiles(@$"{root}/videos")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(Path.GetFileNameWithoutExtension(fileDetails.Filename)));
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var transcript = Directory.GetFiles(@$"{root}/transcripts")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(Path.GetFileNameWithoutExtension(fileDetails.Filename)));
    if (transcript == null)
    {
        return Results.Ok(new
        {
            Success = true,
            Data = new
            {
                VideoUrl = $"{baseUrl}/api/video/{id}",
                TranscriptUrl = "Not Ready",
            }
        });
    }
    return Results.Ok(new
    {
        Success = true,
        Data = new
        {
            VideoUrl = $"{baseUrl}/api/video/{id}",
            TranscriptUrl = $"{baseUrl}/api/transcript/{id}",
        }
    });
});

app.MapGet("/api/video/{id}", async (HngDb db, HttpRequest request, int id) =>
{
    var fileDetails = await db.Files
    .FirstOrDefaultAsync(x => x.Id == id);
    if (fileDetails == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var file = Directory.GetFiles(@$"{root}/videos")
                .FirstOrDefault(x => Path.GetFileName(x).Equals(fileDetails.Filename));
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
    string mimeType = Helpers.GetMimeType(file);

    stream.Position = 0;
    return Results.File(stream, mimeType);
});

app.MapGet("/api/transcript/{id}", async (HngDb db, HttpRequest request, int id) =>
{
    var fileDetails = await db.Files
    .FirstOrDefaultAsync(x => x.Id == id);
    if (fileDetails == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var file = Directory.GetFiles(@$"{root}/transcripts")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(Path.GetFileNameWithoutExtension(fileDetails.Filename)));
    if (file == null)
    {
        return Results.NotFound(new
        {
            Success = false,
            Message = "File not found"
        });
    }

    var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
    string mimeType = Helpers.GetMimeType(file);

    stream.Position = 0;
    return Results.File(stream, mimeType);
});

app.MapPost("/api", async (HngDb db, HttpRequest request) =>
{
    var uploadFile = request.Form.Files[0];

    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(uploadFile.FileName.Replace(" ", ""))
        + "_"
        + DateTime.UtcNow.ToString().Split(" ")[0].Replace(@"/", "_");
    var fileName = fileNameWithoutExtension
        + Path.GetExtension(uploadFile.FileName);
    var files = Directory.GetFiles(@$"{root}/videos").Where(x => x.Contains(fileNameWithoutExtension)).ToList();
    if (files.Count > 0)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = "Filename already exists"
        });
    }
    using (Stream stream = uploadFile.OpenReadStream())
    {
        FileStream fileStream = File.Create(@$"{root}/videos/{fileName}", (int)stream.Length);
        byte[] bytesInStream = new byte[stream.Length];
        stream.Read(bytesInStream, 0, bytesInStream.Length);
        fileStream.Write(bytesInStream, 0, bytesInStream.Length);
        fileStream.Close();
        FileDetails newFile = new()
        {
            Filename = fileName
        };
        await db.Files.AddAsync(newFile);
        await db.SaveChangesAsync();

        string queueName = "hngstage5";
        string connectionString = builder.Configuration.GetConnectionString("StorageConnectionString");
        // Instantiate a QueueClient to create and interact with the queue
        QueueClient queueClient = new(connectionString, queueName);
        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName)));
        return Results.Created($"https://hngstage5.azurewebsites.net/api/{newFile.Id}", new
        {
            Status = "success",
            Data = new
            {
                Name = fileName,
                VideoUrl = $"https://hngstage5.azurewebsites.net/api/video/{newFile.Id}",
                TranscriptUrl = "Not Ready"
            }
        });
    }
});

app.Run();