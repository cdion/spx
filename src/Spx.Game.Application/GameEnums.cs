namespace Spx.Game.Application;

public enum GameStatus
{
    Open = 0,
    Ended = 1
}

public enum GameMessageSenderKind
{
    Player = 0,
    Game = 1
}

public enum GameMessageKind
{
    PlayerPublic = 0,
    PlayerPrivate = 1,
    GameCreated = 2,
    PlayerJoined = 3,
    PlayerLeft = 4,
    GameEnded = 5
}