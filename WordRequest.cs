namespace Wordapp;

// Model to parse the NewWord POST route request body (Note that a record would do just as well, this is just an example o how a regular class is typically defined for a post body)
public class WordRequest
{
    public string Word { get; set; }
}
