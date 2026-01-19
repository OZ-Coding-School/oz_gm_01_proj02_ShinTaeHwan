namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 조준 상태 열거형
    /// </summary>
    public enum AimState
    {
        /// <summary>
        /// 힙파이어 (지향 사격) 상태
        /// </summary>
        HipFire,

        /// <summary>
        /// 힙파이어에서 ADS로 전환 중
        /// </summary>
        TransitioningToADS,

        /// <summary>
        /// ADS (조준) 상태
        /// </summary>
        ADS,

        /// <summary>
        /// ADS에서 힙파이어로 전환 중
        /// </summary>
        TransitioningToHipFire
    }
}
