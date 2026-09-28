using FriendsCorner.Core.Engines.Games;

namespace FriendsCorner.Core.Engines.LetterTiles;

public sealed record PlayTiles(IReadOnlyList<PlacedTile> Tiles) : GameMove;

// A blank being swapped is Tile.Blank.
public sealed record ExchangeTiles(IReadOnlyList<Tile> Tiles) : GameMove;

public sealed record PassTurn : GameMove;

public sealed record PreviewTiles(IReadOnlyList<PlacedTile> Tiles) : GameQuestion;

// Both answers echo the asked tiles so the page can drop an answer it has outgrown.
public sealed record TilesPreview(IReadOnlyList<PlacedTile> Tiles, IReadOnlyList<string> Words, int Score) : GameAnswer;

public sealed record TilesPreviewRefused(IReadOnlyList<PlacedTile> Tiles, Refusal Refusal) : GameAnswer;
