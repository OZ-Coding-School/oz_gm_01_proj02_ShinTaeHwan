using UnityEngine;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Enemy
{
    /// <summary>
    /// 적 중앙 관리자 - EnemyData 보관 및 제공
    /// 다른 Enemy 컴포넌트들은 이 컴포넌트를 통해 데이터에 접근합니다.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyData enemyData;
        public EnemyData Data => enemyData;

        /// <summary>
        /// 런타임에 EnemyData 초기화 (스폰 매니저 등에서 호출)
        /// </summary>
        public void Initialize(EnemyData data)
        {
            enemyData = data;
        }
    }
}
