using Wordapp;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

Database database = new();
NpgsqlDataSource db = database.Connection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files from wwwroot
app.UseDefaultFiles(); // Enables serving index.html as the default file
app.UseStaticFiles(); // Serves static files like CSS, JS, images, etc.

// app.UseHttpsRedirection();

async Task<bool> TestWord(string word)
{
    await using var cmd = db.CreateCommand("SELECT EXISTS (SELECT 1 FROM words WHERE word = $1)");
    cmd.Parameters.AddWithValue(word);
    bool result = (bool)(await cmd.ExecuteScalarAsync() ?? false);
    return result;
}

app.MapGet("/test-word/{word}", TestWord);

async Task<bool> NewWord(string word)
{
    await using var cmd = db.CreateCommand("INSERT INTO words (word) VALUES ($1)");
    cmd.Parameters.AddWithValue(word);
    int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Returns the number of rows affected
    return rowsAffected > 0; // Return true if the insert was successful
}

app.MapPost("/new-word", async (HttpContext context) =>
{
    var requestBody = await context.Request.ReadFromJsonAsync<WordRequest>();
    if (requestBody?.Word is null)
    {
        return Results.BadRequest("Word is required.");
    }
    bool success = await NewWord(requestBody.Word);
    return success ? Results.Ok("Word added successfully.") : Results.StatusCode(500);
});

app.Run();

// Model to parse the POST request body
public class WordRequest
{
    public string Word { get; set; }
}
