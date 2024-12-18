using MyBackend;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseHttpsRedirection();

async Task<bool> TestWord(string word)
{
    bool result = false;
    await using (var cmd = db.CreateCommand("SELECT COUNT(*) FROM words WHERE word = $1"))
    {
        cmd.Parameters.AddWithValue(word);
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine("Found" + reader.GetString(1));
        }
    }
    return result;
}

app.MapGet("/testword/{word}", TestWord);

app.Run();
