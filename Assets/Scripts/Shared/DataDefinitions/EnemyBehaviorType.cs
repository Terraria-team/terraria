namespace Shared.DataDefinitions
{
    /// Fighter  → Н1, П1, Д1 (ground melee)
    /// Flyer    → Л1, Л2     (flying, optional sinusoidal)
    /// Slime    → Н1 variant  (idle + jump impulse)
    /// Shooter  → П2, П3     (ranged, LoS-based)
    public enum EnemyBehaviorType : byte
    {
        Fighter,
        Flyer,
        Slime,
        Shooter
    }
}
