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
        
        // Map incomming request to add a game
        app.MapPost("/add-game", async (HttpContext context) =>
        {
            // Game here, is a class that defines the post requestBody format
            var requestBody = await context.Request.ReadFromJsonAsync<Game>();
            if (requestBody?.player_1 is null || requestBody?.player_2 is null || requestBody?.gamecode is null)
            {
                return Results.BadRequest("player_1, player_2 and gamecode are required.");
            }
            Game game = await AddGame(requestBody.player_1, requestBody.player_2, requestBody.gamecode);
            return (game.id > 0) ? Results.Ok(game) : Results.StatusCode(500);
        });

        // Map incomming request to add a player to a game
        app.MapPost("/add-player", async (HttpContext context) =>
        {
            // Player here, is a class that defines the post requestBody format
            var requestBody = await context.Request.ReadFromJsonAsync<Player>();
            if (requestBody?.name is null)
            {
                return Results.BadRequest("name is required.");
            }
            Player player = await AddPlayer(requestBody.name, context.Request.Cookies["ClientId"]);
            return (player.Id > 0) ? Results.Ok(player) : Results.StatusCode(500);
        });

        // Map incomming request to get players for a game
        app.MapGet("/players/{id}", GetPlayers);
        
        // Map incomming request to play a tile (make a move) in a game
        app.MapPost("/play-tile", async (HttpContext context) =>
        {
            var requestBody = await context.Request.ReadFromJsonAsync<Move>();
            if (requestBody?.player is null || requestBody?.tile is null ||requestBody?.game is null)
            {
                return Results.BadRequest("player (id), tile (index) and game (id) is required.");
            }
            bool success = await PlayTile(requestBody.player, requestBody.tile, requestBody.game);
            return success ? Results.Ok("A new move was made, played a tile.") : Results.StatusCode(500);
        });
        
        // Map incomming request to check win for a player in a game
        app.MapGet("/check-win/{player}/{game}", CheckWin);       
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
    async Task<Player> AddPlayer(string name, string clientId)
    {
        // check if player already exists
        await using var cmd = db.CreateCommand("SELECT * FROM players WHERE name = $1"); // check if player exists
        cmd.Parameters.AddWithValue(name);
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var dbClientId = reader.GetString(1);
                if (clientId.Equals(dbClientId) == false)
                {
                    // save new clientId to db
                    await using var cmd2 = db.CreateCommand("UPDATE players SET clientid = $1 WHERE id = $2");
                    cmd2.Parameters.AddWithValue(clientId);
                    cmd2.Parameters.AddWithValue(reader.GetInt32(2));
                    await cmd2.ExecuteNonQueryAsync(); // Perform update
                }
                return new Player(reader.GetString(0), clientId, reader.GetInt32(2));
            }
        }
        // if player did not exist we create them
        await using var cmd3 = db.CreateCommand("INSERT INTO players (name, clientid) VALUES ($1, $2) RETURNING id");
        cmd3.Parameters.AddWithValue(name);
        cmd3.Parameters.AddWithValue(clientId);
        var result = await cmd3.ExecuteScalarAsync();
        if (result != null && int.TryParse(result.ToString(), out int lastInsertedId))
        {
            return new Player(name, clientId, lastInsertedId);
        }
        else
        {
            Console.WriteLine("Failed to retrieve the last inserted ID.");
            return null;
        }
    }

    async Task<Game> AddGame(int player_1, int player_2, string gamecode)
    {
        // check if game already exists
        await using var cmd = db.CreateCommand("SELECT * FROM games WHERE gamecode = $1"); // check if game exists
        cmd.Parameters.AddWithValue(gamecode);
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                return new Game(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetString(4));
            }
        }
        // if game did not exist we create it
        await using var cmd3 = db.CreateCommand("INSERT INTO games (player_1, player_2, players_turn, gamecode) VALUES ($1, $2, $3, $4) RETURNING id");
        cmd3.Parameters.AddWithValue(player_1);
        cmd3.Parameters.AddWithValue(player_2);
        cmd3.Parameters.AddWithValue(player_1); // player 1 starts
        cmd3.Parameters.AddWithValue(gamecode);
        var result = await cmd3.ExecuteScalarAsync();
        if (result != null && int.TryParse(result.ToString(), out int lastInsertedId))
        {
            return new Game(lastInsertedId, player_1, player_2, player_1, gamecode);
        }
        else
        {
            Console.WriteLine("Failed to retrieve the last inserted ID.");
            return null;
        }
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
    async Task<bool> PlayTile(int player, int tile, int game)
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
        if (rowsAffected > 0)
        {
            return true; // Return true if the move was successful   
        }
        return false;
    }

    async Task<List<int>?> CheckWin(int player, int game)
    {
        
        // Defining wins, using a list of Tuples with indices. A Tuple is a read only, fixed size, list-type structure.
        // The indices are a serialization of the tiles in our tictactoe game with the top left index being 0 and the bottom right being 8.
        // Serializing game boards like this is a common and practical solution. 
        var winningVectors = new List<Tuple<int, int, int>>
        {
            // Horizontal wins 
            Tuple.Create(0, 1, 2),
            Tuple.Create(3, 4, 5),
            Tuple.Create(6, 7, 8),
            
            // Vertical wins
            Tuple.Create(0, 3, 6),
            Tuple.Create(1, 4, 7),
            Tuple.Create(2, 5, 8),
            
            // Diagonal wins
            Tuple.Create(0, 4, 8),
            Tuple.Create(2, 4, 6)
        };
        
        // Get all the tiles the current player has placed so far in the game
        var currentPlayerTiles = new List<int>();
        await using var cmd = db.CreateCommand("SELECT tile FROM moves WHERE game = $1 AND player = $2");
        cmd.Parameters.AddWithValue(game);
        cmd.Parameters.AddWithValue(player);
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                currentPlayerTiles.Add(reader.GetInt32(0));
            }
        }
        
        // Now lets see if the current player has a win
        foreach (var vector in winningVectors)
        {
            if (currentPlayerTiles.Contains(vector.Item1) && currentPlayerTiles.Contains(vector.Item2) &&
                currentPlayerTiles.Contains(vector.Item3))
            {
                Console.WriteLine($"Winning vector: {vector.Item1}, {vector.Item2}, {vector.Item3}");
                // if we have a match, return the winning vector as a confirmation of the win
                var winningVector = new List<int>();
                winningVector.Add(vector.Item1);
                winningVector.Add(vector.Item2);
                winningVector.Add(vector.Item3);
                return winningVector;
            }
        }
        // if we don't have a match, return null
        return null;
    }
    
}

