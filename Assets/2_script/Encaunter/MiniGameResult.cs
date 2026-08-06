using System.Collections.Generic;

public sealed class MiniGameResult
{
    public MiniGameId Game { get; }
    public bool IsCompleted { get; }
    public int CoinsAwarded { get; }
    public int ScorePenalty { get; }
    public IReadOnlyList<Cargo> AwardedCargo { get; }

    public MiniGameResult(
        MiniGameId game,
        bool isCompleted,
        int coinsAwarded,
        int scorePenalty,
        IReadOnlyList<Cargo> awardedCargo)
    {
        Game = game;
        IsCompleted = isCompleted;
        CoinsAwarded = coinsAwarded;
        ScorePenalty = scorePenalty;
        AwardedCargo = awardedCargo ?? new Cargo[0];
    }
}
