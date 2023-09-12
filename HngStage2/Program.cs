using HngStage2.Models;
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

app.Map("/api", async (HngDb db, Person? updateperson, HttpRequest request) =>
{
    try
    {
        List<string> queryArray = new();
        var query = $"SELECT * FROM people WHERE ";
        string[] components = new string[request.Query.Count];

        var counter = 0;
        foreach (var item in request.Query)
        {
            queryArray.Add($"{item.Key} = " + "{" + counter + "}");
            components[counter] = item.Value.ToString();
            counter++;
        }

        query += string.Join(" AND ", queryArray);

        var person = db.People.FromSqlRaw<Person>(query, components).FirstOrDefault();
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


app.MapGet("/api/{user}", async (HngDb db, string user) =>
{
    if (user == null) return Results.BadRequest();
    int user_id;
    Person? person;
    if (int.TryParse(user, out user_id))
    {
        person = await db.People.FindAsync(user_id);
    }
    else
    {
        person = await db.People.Where(x => x.Name == user).FirstOrDefaultAsync();
    }

    if (person == null) return Results.NotFound();
    return Results.Ok(person);
});

app.MapPut("/api/{user_id}", async (HngDb db, Person updateperson, int user_id) =>
{
    var person = await db.People.FindAsync(user_id);
    if (person is null) return Results.NotFound();
    person.Name = updateperson.Name;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("UpdatePerson");

app.MapDelete("/api/{user_id}", async (HngDb db, int user_id) =>
{
    var person = await db.People.FindAsync(user_id);
    if (person is null) return Results.NotFound();
    db.People.Remove(person);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();