namespace Shared.DataDefinitions
{
    /// Fighter      → Н1, П1, Д1  (ground melee)
    /// Flyer        → Л1           (flying melee, optional sinusoidal)
    /// Slime        → Н1 variant   (idle + jump impulse)
    /// Shooter      → П2, П3      (ground ranged, LoS-based)
    /// FlyerShooter → Л2           (flying ranged, ignores gravity, shoots)
    public enum EnemyBehaviorType : byte
    {
        Fighter,
        Flyer,
        Slime,
        Shooter,
        FlyerShooter
    }
}
