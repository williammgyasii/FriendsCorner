using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Core.Accessors;

public sealed record CaseRequest(string Theme, MysteryLevel Level);

// Writes one murder-mystery case. Returns null when it cannot; it never
// throws into the room. Whether the case is fair is not its call.
public interface ICaseWriterAccessor
{
    Task<Case?> Write(CaseRequest request, CancellationToken cancellationToken);
}
