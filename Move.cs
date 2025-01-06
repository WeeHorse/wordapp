namespace Wordapp;

// (Note that a normal class would do just as well, this is just an example of how a record is typically defined for a post body)
public record Move(int player, int tile, int game);