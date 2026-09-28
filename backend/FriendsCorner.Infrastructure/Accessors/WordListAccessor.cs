using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Infrastructure.Accessors;

// ENABLE ships inside this assembly. It is read once, on the first check,
// so rooms that never play Letter Tiles never pay for it.
public sealed class WordListAccessor : IWordListAccessor
{
    private readonly Lazy<HashSet<string>> _words = new(Load);

    public bool Contains(string word) => _words.Value.Contains(word);

    private static HashSet<string> Load()
    {
        using var stream = typeof(WordListAccessor).Assembly.GetManifestResourceStream("enable1.txt")
            ?? throw new InvalidOperationException("The enable1.txt word list is not embedded.");
        using var reader = new StreamReader(stream);

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
        {
            if (line.Trim() is { Length: > 0 } word)
            {
                words.Add(word);
            }
        }

        return words;
    }
}
