namespace FriendsCorner.Core.Engines.Mystery;

// Where a case is set, chosen by the server so the writer doesn't settle on
// the same manor house every time.
public static class CaseThemes
{
    public static readonly IReadOnlyList<string> All =
    [
        "a snowed-in coaching inn on the moors",
        "a sleeper train crossing the Alps",
        "a 1920s ocean liner on its maiden voyage",
        "a vineyard's harvest party",
        "a village baking contest",
        "a theatre on opening night",
        "a lighthouse island cut off by a storm",
        "a 1930s jazz club",
        "a botanical garden's midnight gala",
        "a museum after hours",
        "a ski chalet during a blizzard",
        "a seaside hotel at the end of the season",
    ];

    public static string Pick(Random random) => All[random.Next(All.Count)];
}
