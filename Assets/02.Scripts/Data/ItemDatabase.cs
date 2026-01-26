using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// 게임 내 모든 아이템을 관리하는 데이터베이스
    /// 저장/로드 시 이름 기반으로 아이템을 찾기 위해 사용
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "MiniExtractionShooter/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        private static ItemDatabase _instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Debug.Log("[ItemDatabase] Instance 첫 접근 - Resources에서 로드 시도...");
                    _instance = Resources.Load<ItemDatabase>("ItemDatabase");

                    if (_instance == null)
                    {
                        // Resources 폴더에 없을 경우 경고
                        // Debug.LogError("[ItemDatabase] 'ItemDatabase' asset not found in Resources folder! Please create it.");
                    }
                    else
                    {
                        // Debug.Log("[ItemDatabase] ItemDatabase.asset 로드 성공. Initialize() 호출...");
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        [Header("Registered Items")]
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();

        // 런타임 검색 최적화를 위한 딕셔너리
        private Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();
        
        [System.NonSerialized]
        private bool isInitialized = false;

        /// <summary>
        /// 딕셔너리 초기화
        /// </summary>
        public void Initialize()
        {
            // 딕셔너리가 비어있다면 강제로 재초기화
            if (isInitialized && itemLookup.Count > 0) return;

            // Fallback: 리스트가 비어있으면 Resources에서 로드 시도
            if (allItems.Count == 0)
            {
                // Debug.LogWarning("[ItemDatabase] allItems list is empty! Attempting fallback to Resources.LoadAll.");
                var resourcesItems = Resources.LoadAll<ItemData>("");
                // Debug.Log($"[ItemDatabase] Fallback loaded {resourcesItems.Length} items from Resources.");
                allItems.AddRange(resourcesItems);
            }

            itemLookup.Clear();
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    if (!itemLookup.ContainsKey(item.itemName))
                    {
                        itemLookup.Add(item.itemName, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemDatabase] Duplicate item name found: {item.itemName}. Ignoring {item.name}.");
                    }
                }
            }

            isInitialized = true;
            // Debug.Log($"[ItemDatabase] Initialized with {itemLookup.Count} items.");
        }

        /// <summary>
        /// 이름으로 아이템 찾기
        /// </summary>
        public ItemData GetItemByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) 
            {
                Debug.LogWarning("[ItemDatabase] GetItemByName: itemName is null or empty");
                return null;
            }
            // 아직 초기화되지 않았다면 초기화 (Instance 호출 시 되지만 안전장치)
            if (!isInitialized) Initialize();

            if (itemLookup.TryGetValue(itemName, out ItemData item))
            {
                return item;
            }

            Debug.LogWarning($"[ItemDatabase] Item not found: {itemName}");
            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 프로젝트의 모든 ItemData를 찾아 리스트에 등록 (Editor Only)
        /// 인스펙터 컨텍스트 메뉴에서 실행 가능
        /// </summary>
        [ContextMenu("Load All Items")]
        public void LoadAllItems()
        {
            Undo.RecordObject(this, "Load All Items");
            
            allItems.Clear();
            
            // ItemData 타입의 모든 에셋 GUID 검색
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            
            int addedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                
                if (item != null)
                {
                    allItems.Add(item);
                    addedCount++;
                }
            }
            
            // Debug.Log($"[ItemDatabase] Successfully loaded {addedCount} items from project.");
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
