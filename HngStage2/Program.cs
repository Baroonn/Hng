using HngStage2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Hng") ?? "Data Source=Hng.db";
builder.Services.AddSqlite<HngDb>(connectionString);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HngDb>();
    db.Database.Migrate();
}

app.MapPost("/api", async (HngDb db, Person person) =>
{
    if (string.IsNullOrWhiteSpace(person.Name)) return Results.BadRequest();
    Person newPerson = new();
    newPerson.Name = person.Name;
    await db.People.AddAsync(newPerson);
    await db.SaveChangesAsync();
    return Results.Created($"/api/{newPerson.Id}", newPerson);
});

app.Map("/api/{user}", async (HngDb db, string user, [FromBody] Person? updateperson, HttpRequest request) =>
{
    try
    {
        if (user == null) return Results.BadRequest();
        Person? person;
        if (int.TryParse(user, out int user_id))
        {
            person = await db.People.FindAsync(user_id);
        }
        else
        {
            person = await db.People.Where(x => x.Name == user).FirstOrDefaultAsync();
        }
        if (person == null) return Results.NotFound();
        if (request.Method == "GET") return Results.Ok(person);
        else if (request.Method == "PUT" && updateperson != null) person.Name = updateperson.Name;
        else if (request.Method == "PUT" && updateperson == null) return Results.BadRequest();
        else if (request.Method == "DELETE") db.People.Remove(person);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (SqliteException ex)
    {
        return Results.BadRequest();
    }
    catch
    {
        return Results.StatusCode(500);
    }
});


app.Run();