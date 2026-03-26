interface ITimedBoost : IBoost
{
    float Duration { get; }
    void Tick(float deltaTime);
    void Deactivate();
}