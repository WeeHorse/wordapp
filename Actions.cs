using Npgsql;

namespace Wordapp;

public class Actions
{

    Database database = new();
    private NpgsqlDataSource db;
    public Actions(WebApplication app)
    {
        db = database.Connection();

        // Map incomming TestWord GET route from client to method
        // http://localhost:5185/test-word/Smurfa
        app.MapGet("/test-word/{word}", TestWord);

        // Map incomming NewWord POST route from client to method
        app.MapPost("/new-word", async (HttpContext context) =>
        {
            // WordRequest here, is a class that defines the post requestBody format
            var requestBody = await context.Request.ReadFromJsonAsync<WordRequest>();
            if (requestBody?.Word is null)
            {
                return Results.BadRequest("Word is required.");
            }
            bool success = await NewWord(requestBody.Word, context.Request.Cookies["ClientId"]);
            return success ? Results.Ok("Word added successfully.") : Results.StatusCode(500);
        });

        // Map incomming request to add a player to a game
        app.MapPost("/add-player", async (HttpContext context) =>
        {
            // WordRequest here, is a class that defines the post requestBody format
            var requestBody = await context.Request.ReadFromJsonAsync<Player>();
            if (requestBody?.name is null)
            {
                return Results.BadRequest("name is required.");
            }
            bool success = await AddPlayer(requestBody.name, context.Request.Cookies["ClientId"]);
            return success ? Results.Ok("Player added successfully.") : Results.StatusCode(500);
        });

        // Map incomming request to get players for a game
        app.MapGet("/players/{id}", GetPlayers);
        
        // Map incomming request to play a tile (make a move) in a game
        app.MapPost("/play-tile", async (HttpContext context) =>
        {
            var requestBody = await context.Request.ReadFromJsonAsync<Move>();
            if (requestBody?.player is null || requestBody?.game is null || requestBody?.tile is null)
            {
                return Results.BadRequest("player (id), game (id) and tile (index) is required.");
            }
            bool success = await PlayTile(requestBody.tile, requestBody.player, requestBody.game);
            return success ? Results.Ok("A new move was made, played a tile.") : Results.StatusCode(500);
        });

    }

    // Process incomming TestWord from client
    async Task<bool> TestWord(string word)
    {
        await using var cmd = db.CreateCommand("SELECT EXISTS (SELECT 1 FROM words WHERE word = $1)"); // fast if word exists in table query 
        cmd.Parameters.AddWithValue(word);
        bool result = (bool)(await cmd.ExecuteScalarAsync() ?? false); // Execute fast if word exists in table query 
        return result;
    }


    // Process incomming NewWord  from client
    async Task<bool> NewWord(string word, string clientId)
    {
        await using var cmd = db.CreateCommand("INSERT INTO words (word, clientid) VALUES ($1, $2)");
        cmd.Parameters.AddWithValue(word);
        cmd.Parameters.AddWithValue(clientId);
        int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Returns the number of rows affected
        return rowsAffected > 0; // Return true if the insert was successful
    }

    // Process incomming AddPlayer from client
    async Task<bool> AddPlayer(string name, string clientId)
    {
        // check if player already exists
        await using var cmd = db.CreateCommand("INSERT INTO players (name, clientid) VALUES ($1, $2)");
        cmd.Parameters.AddWithValue(name);
        cmd.Parameters.AddWithValue(clientId);
        int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Returns the number of rows affected
        return rowsAffected > 0; // Return true if the insert was successful
    }

    // Process incomming GetPlayers from client
    async Task<List<Player>> GetPlayers(int id)
    {
        var players = new List<Player>();
        await using var cmd = db.CreateCommand("SELECT * FROM players, games WHERE games.id = $1 AND players.id IN (games.player_1, games.player_2)"); // get players from a game
        cmd.Parameters.AddWithValue(id);
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                players.Add(new Player(reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
            }
        }

        return players;
    }
    
    // Process incomming PlayTile from client
    async Task<bool> PlayTile(int tile, int player, int game)
    {
        Console.WriteLine($"Playing tile {tile} from {player} to {game}");
        await using var cmd1 = db.CreateCommand("SELECT EXISTS (SELECT 1 FROM moves WHERE tile = $1 AND game = $2)"); // fast if move exists in table query 
        cmd1.Parameters.AddWithValue(tile);
        cmd1.Parameters.AddWithValue(game);
        bool result = (bool)(await cmd1.ExecuteScalarAsync() ?? false); // Execute fast if move exists in table query 
        Console.WriteLine($"Player {player} played at {tile} in game {game} with result {result}");
        if (result)
        {
            return false; // Return false if the move was unsuccessful
        }
        
        await using var cmd = db.CreateCommand("INSERT INTO moves (tile, player, game) VALUES ($1, $2, $3)");
        cmd.Parameters.AddWithValue(tile);
        cmd.Parameters.AddWithValue(player);
        cmd.Parameters.AddWithValue(game);
        int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Returns the number of rows affected
        return rowsAffected > 0; // Return true if the move was successful
    }
}

