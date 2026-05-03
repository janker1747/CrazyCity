interface ITimedBoost : IBoost
{
    float Duration { get; }
    void Tick(float deltaTime);
    bool IsFinished { get; }
    void Deactivate();
}