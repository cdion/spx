namespace Spx.Web.Playground.Nexus;

internal static class PlaygroundNexusUsers
{
    public const string Player1UserId = "playground-player-1";
    public const string Player2UserId = "playground-player-2";

    public static readonly Guid Player1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Player2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public static readonly IReadOnlyDictionary<Guid, string> PlayerNames = new Dictionary<
        Guid,
        string
    >
    {
        [Player1Id] = "Player One",
        [Player2Id] = "Player Two",
    };

    public static readonly IReadOnlyList<Guid> PlayerIds = [Player1Id, Player2Id];

    public static bool TryGetViewer(string userId, out Guid playerId, out string playerName)
    {
        if (userId == Player1UserId)
        {
            playerId = Player1Id;
            playerName = PlayerNames[Player1Id];
            return true;
        }

        if (userId == Player2UserId)
        {
            playerId = Player2Id;
            playerName = PlayerNames[Player2Id];
            return true;
        }

        playerId = Guid.Empty;
        playerName = string.Empty;
        return false;
    }

    public static string GetPlayerName(Guid playerId) =>
        PlayerNames.TryGetValue(playerId, out var playerName)
            ? playerName
            : playerId.ToString("N")[..8];
}
