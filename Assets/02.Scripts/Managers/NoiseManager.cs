using UnityEngine;
using System;

namespace MiniExtractionShooter.Managers
{
    /// <summary>
    /// AI 감지용 소음 관리자
    /// </summary>
    public static class NoiseManager
    {
        // 소음 발생 이벤트 (위치, 범위)
        public static event Action<Vector3, float> OnNoiseGenerated;

        /// <summary>
        /// 소음 발생
        /// </summary>
        /// <param name="position">소음 발생 위치</param>
        /// <param name="range">소음 감지 범위 (m)</param>
        public static void MakeNoise(Vector3 position, float range)
        {
            // Debug.Log($"[NoiseManager] Noise generated at {position} with range {range}");
            OnNoiseGenerated?.Invoke(position, range);
            
        }
    }
}
