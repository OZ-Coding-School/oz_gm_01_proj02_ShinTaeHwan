using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MiniExtractionShooter.Loot;
using MiniExtractionShooter.Core;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 루팅 UI
    /// TDD 기반 아이템 순차 공개 시스템
    /// </summary>
    public class LootUI : MonoBehaviour
    {
        public static LootUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject lootPanel;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Item Slots")]
        [SerializeField] private Transform itemSlotContainer;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private int maxVisibleSlots = 6;

        [Header("Hints")]
        [SerializeField] private TextMeshProUGUI hintText;

        private LootableObject currentLootable;
        private List<LootSlotUI> activeSlots = new List<LootSlotUI>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Close();
        }

        /// <summary>
        /// 루팅 UI 열기
        /// </summary>
        public void Open(LootableObject lootable)
        {
            if (lootable == null) return;

            currentLootable = lootable;

            if (lootPanel != null)
            {
                lootPanel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = "루팅 중...";
            }

            // 슬롯 생성
            CreateSlots(lootable.Items.Count);

            // 힌트 텍스트
            UpdateHintText();

            // 이벤트 구독
            lootable.OnItemRevealed += HandleItemRevealed;
            lootable.OnItemTaken += HandleItemTaken;
        }

        /// <summary>
        /// 루팅 UI 닫기
        /// </summary>
        public void Close()
        {
            if (currentLootable != null)
            {
                currentLootable.OnItemRevealed -= HandleItemRevealed;
                currentLootable.OnItemTaken -= HandleItemTaken;
                currentLootable = null;
            }

            if (lootPanel != null)
            {
                lootPanel.SetActive(false);
            }

            ClearSlots();
        }

        /// <summary>
        /// 슬롯 생성
        /// </summary>
        private void CreateSlots(int count)
        {
            ClearSlots();

            int slotCount = Mathf.Min(count, maxVisibleSlots);

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj;
                LootSlotUI slot = null;

                if (itemSlotPrefab != null)
                {
                    // Get LootSlotUI component from prefab for pooling
                    LootSlotUI prefabComponent = itemSlotPrefab.GetComponent<LootSlotUI>();
                    if (prefabComponent != null)
                    {
                        slot = PoolManager.Instance.GetFromPool(prefabComponent);
                        if (slot != null)
                        {
                            slotObj = slot.gameObject;
                            slotObj.transform.SetParent(itemSlotContainer);
                        }
                        else
                        {
                            slotObj = CreateDefaultSlot();
                            slotObj.transform.SetParent(itemSlotContainer);
                            slot = slotObj.GetComponent<LootSlotUI>();
                        }
                    }
                    else
                    {
                        slotObj = CreateDefaultSlot();
                        slotObj.transform.SetParent(itemSlotContainer);
                        slot = slotObj.GetComponent<LootSlotUI>();
                    }
                }
                else
                {
                    slotObj = CreateDefaultSlot();
                    slotObj.transform.SetParent(itemSlotContainer);
                    slot = slotObj.GetComponent<LootSlotUI>();
                }

                if (slot == null)
                {
                    slot = slotObj.AddComponent<LootSlotUI>();
                }

                slot.SetIndex(i);
                slot.SetHidden();
                slot.OnSlotClicked += HandleSlotClicked;

                activeSlots.Add(slot);
            }
        }

        /// <summary>
        /// 슬롯 클리어
        /// </summary>
        private void ClearSlots()
        {
            foreach (var slot in activeSlots)
            {
                slot.OnSlotClicked -= HandleSlotClicked;

                // Only return to pool if prefab has LootSlotUI component
                if (itemSlotPrefab != null && itemSlotPrefab.GetComponent<LootSlotUI>() != null)
                {
                    PoolManager.Instance.ReturnPool(slot);
                }
                else
                {
                    Destroy(slot.gameObject);
                }
            }
            activeSlots.Clear();
        }

        /// <summary>
        /// 기본 슬롯 생성 (프리팹 없을 때)
        /// </summary>
        private GameObject CreateDefaultSlot()
        {
            GameObject slot = new GameObject("LootSlot");

            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button button = slot.AddComponent<Button>();

            // 아이콘
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10, 30);
            iconRect.offsetMax = new Vector2(-10, -10);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;

            // 이름 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(slot.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, 25);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "?";

            return slot;
        }

        /// <summary>
        /// 아이템 공개 처리
        /// </summary>
        public void RevealItem(int index, LootItem item)
        {
            if (index < 0 || index >= activeSlots.Count) return;

            activeSlots[index].SetItem(item);
        }

        /// <summary>
        /// 아이템 공개 핸들러
        /// </summary>
        private void HandleItemRevealed(int index, LootItem item)
        {
            RevealItem(index, item);
        }

        /// <summary>
        /// 아이템 획득 핸들러
        /// </summary>
        private void HandleItemTaken(LootItem item)
        {
            RefreshUI();
        }

        /// <summary>
        /// 슬롯 클릭 핸들러
        /// </summary>
        private void HandleSlotClicked(int index)
        {
            if (currentLootable != null)
            {
                currentLootable.TakeItem(index);
            }
        }

        /// <summary>
        /// UI 새로고침
        /// </summary>
        public void RefreshUI()
        {
            if (currentLootable == null) return;

            // 슬롯 재생성
            CreateSlots(currentLootable.Items.Count);

            // 공개된 아이템 표시
            for (int i = 0; i < currentLootable.RevealedCount && i < activeSlots.Count; i++)
            {
                if (i < currentLootable.Items.Count)
                {
                    activeSlots[i].SetItem(currentLootable.Items[i]);
                }
            }

            UpdateHintText();
        }

        /// <summary>
        /// 힌트 텍스트 업데이트
        /// </summary>
        private void UpdateHintText()
        {
            if (hintText != null)
            {
                hintText.text = "클릭: 개별 획득 | Space: 모두 획득 | ESC: 닫기";
            }
        }
    }

    /// <summary>
    /// 루팅 슬롯 UI 컴포넌트
    /// </summary>
    public class LootSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        private int slotIndex;
        private bool isRevealed = false;

        public event System.Action<int> OnSlotClicked;

        private void Awake()
        {
            // 컴포넌트 자동 찾기
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (nameText == null)
                nameText = GetComponentInChildren<TextMeshProUGUI>();
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }

        public void SetIndex(int index)
        {
            slotIndex = index;
        }

        public void SetHidden()
        {
            isRevealed = false;

            if (nameText != null)
            {
                nameText.text = "?";
            }

            if (iconImage != null)
            {
                iconImage.color = new Color(1, 1, 1, 0.3f);
                iconImage.sprite = null;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }

        public void SetItem(LootItem item)
        {
            isRevealed = true;

            if (nameText != null)
            {
                nameText.text = item.GetDisplayName();
            }

            if (iconImage != null)
            {
                Sprite icon = item.GetIcon();
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.color = new Color(1, 1, 1, 0.5f);
                }
            }

            if (backgroundImage != null)
            {
                // 타입별 색상
                Color bgColor = item.ItemType switch
                {
                    Data.ItemType.Weapon => new Color(0.5f, 0.3f, 0.1f, 0.8f),
                    Data.ItemType.Armor => new Color(0.1f, 0.3f, 0.5f, 0.8f),
                    Data.ItemType.Ammo => new Color(0.4f, 0.4f, 0.2f, 0.8f),
                    Data.ItemType.Health => new Color(0.2f, 0.5f, 0.2f, 0.8f),
                    Data.ItemType.Valuable => new Color(0.5f, 0.4f, 0.1f, 0.8f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.8f)
                };
                backgroundImage.color = bgColor;
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }

        private void OnClick()
        {
            if (isRevealed)
            {
                OnSlotClicked?.Invoke(slotIndex);
            }
        }
    }
}
