# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mini Extraction Shooter - A casual top-down extraction shooter game built with Unity 3D URP. The player infiltrates an industrial zone, collects loot, fights AI enemies, and must escape to an extraction zone within a time limit. Inspired by "Escape from Duckov".

- **Engine**: Unity (3D URP)
- **Platform**: PC (Windows)
- **Language**: C#
- **Genre**: Casual extraction shooter (top-down perspective)

## Scene Structure & Game Flow

### Scenes
- **Main** (메인 메뉴): 게임 시작 화면. 계속하기, 세이브 삭제, 종료 버튼
- **Home** (홈 베이스): 준비 구역. 창고에 아이템 보관, 총/총알 구매, 장비 정비
- **Game** (전투 씬): 실제 미션 수행. 적 처치, 아이템 루팅, 탈출

### Game Loop
```
Main → (계속하기) → Home → (출발) → Game → (탈출 성공) → Home
                                      ↓ (사망)
                                    Game Over
```

### Scene Details
| Scene | 주요 오브젝트 | 매니저 |
|-------|-------------|--------|
| **Main** | MainMenuController, Canvas | SaveDataManager (DontDestroyOnLoad) |
| **Home** | Player, InventoryCanvas, Shop, Storage | GameManager (씬별) |
| **Game** | Player, InventoryCanvas, Enemies, LootObjects, ExtractionZone, SpawnManager | GameManager (씬별) |

### Shared Objects (Home & Game)
- **Player**: PlayerController, PlayerHealth, PlayerCombat, PlayerAnimator, PlayerInventory
- **InventoryCanvas**: InventoryUI, Equipment slots, Quick slots

## Architecture

### Namespace Structure
All scripts use the `MiniExtractionShooter` namespace with sub-namespaces:
- `MiniExtractionShooter.Core` - Singleton base, SaveDataManager, Object Pooling
- `MiniExtractionShooter.Player` - PlayerController, PlayerHealth, PlayerInventory, PlayerCombat, PlayerAnimator
- `MiniExtractionShooter.Enemy` - EnemyAI, EnemyHealth, EnemyCombat, EnemyDropSystem
- `MiniExtractionShooter.Managers` - GameManager, UIManager
- `MiniExtractionShooter.Combat` - DamageCalculator, HitZone, HitboxManager
- `MiniExtractionShooter.Weapon` - WeaponBase, WeaponManager, RecoilSystem, AimingSystem, SpreadSystem
- `MiniExtractionShooter.Loot` - LootItem, LootBox, LootInteraction
- `MiniExtractionShooter.UI` - HUDController, ExtractionUI, DynamicCrosshair
- `MiniExtractionShooter.UI.Inventory` - InventoryUI, InventorySlot, EquipmentSlot, DragItem
- `MiniExtractionShooter.Level` - ExtractionZone, SpawnPoint, CameraFollow, BuildingTransparency
- `MiniExtractionShooter.Data` - ScriptableObject definitions (WeaponData, EnemyData, ArmorData, ItemData, AmmoData, LootTableData, GameSaveData)

### Core Patterns
- **Singleton Pattern**: Use `Singleton<T>` base class from `Core/Singleton.cs` for managers. Supports `dontDestroyOnLoad` option.
- **ScriptableObject Data**: Weapon, Enemy, Armor, Item, Ammo, and LootTable configurations use ScriptableObjects in `Data/`
- **Event-Driven**: Components communicate via C# events (e.g., `OnDeath`, `OnGameStateChanged`, `OnFired`, `OnAmmoChanged`)
- **Object Pooling**: `PoolManager` singleton manages object pools for bullets, effects. Use `CreatePool<T>()`, `GetFromPool<T>()`, `ReturnPool<T>()`

### Key Systems
- **GameManager**: Controls game state (MainMenu, Playing, Paused, GameOver, Victory), handles scene transitions, tracks statistics (kills, items, playtime)
- **PlayerController**: WASD movement + mouse aim in top-down view, uses CharacterController. Static `Instance` property (not Singleton base).
- **Combat System**: Damage formula: `(baseDamage * hitZoneMultiplier - armor) * distanceFalloff`. Uses raycast with HitZone components.
- **Weapon System**: WeaponBase handles firing, reloading, ammo. RecoilSystem/SpreadSystem/AimingSystem manage accuracy. Supports consecutive shot recoil multiplier.
- **Enemy AI**: NavMesh-based with state machine (Idle -> Alert -> Chase -> Attack). Uses `EnemyData` ScriptableObject for stats.
- **Save System**: `SaveDataManager` saves/loads JSON to `Application.persistentDataPath`. Stores inventory, weapons, statistics.
- **Looting**: F key interaction, player immobilized during looting via `PlayerController.SetCanMove(false)`
- **Extraction**: 5-second timer in extraction zone triggers victory and saves game

## Script Organization

```
Assets/02.Scripts/
├── Core/
│   ├── Singleton.cs          # Generic Singleton<T> base class
│   ├── SaveDataManager.cs    # JSON save/load system
│   └── Pooling/              # PoolManager, ObjectPool
├── Data/                     # ScriptableObjects (WeaponData, EnemyData, ArmorData, ItemData, etc.)
├── Player/                   # PlayerController, PlayerHealth, PlayerInventory, PlayerCombat, PlayerAnimator
├── Enemy/                    # EnemyAI, EnemyHealth, EnemyCombat, EnemyDropSystem
├── Weapon/                   # WeaponBase, WeaponManager, RecoilSystem, AimingSystem, SpreadSystem
├── Combat/                   # DamageCalculator, HitZone, HitboxManager
├── Loot/                     # LootItem, LootBox, LootInteraction
├── Level/                    # ExtractionZone, SpawnPoint, CameraFollow, BuildingTransparency
├── Managers/                 # GameManager, UIManager
└── UI/
    ├── HUDController.cs, ExtractionUI.cs, DynamicCrosshair.cs
    └── Inventory/            # InventoryUI, InventorySlot, EquipmentSlot, DragItem
```

## Key Balance Values

- Player health: 100, walk speed: 5m/s, run speed: 8m/s
- Pistol: 20 damage, 300 RPM, 12 rounds, 15m effective range
- Rifle: 25 damage, 600 RPM, 30 rounds, 25m effective range
- Hit zone multipliers: Head 2.5x, Body 1.0x, Arms 0.7x, Legs 0.8x
- Enemy (Guard): 60 HP, 3.5m/s speed, 12m detection range, 60% accuracy
- Extraction time: 5 seconds

## Controls

- WASD: Movement
- Shift: Run
- Mouse: Aim direction
- Left Click: Fire
- R: Reload
- F: Loot interaction
- Tab/I: Inventory
- ESC: Pause/Cancel looting
- 1, 2: Weapon switch

## UI Canvas Structure (Map_v1.unity)

> Canvas Rebuild 최적화를 위해 업데이트 빈도에 따라 Canvas를 분리합니다.

### Canvas (Main HUD) - Sort Order: 0
- **업데이트 빈도**: 매 프레임 (HP, 탄약, 크로스헤어)
- **Render Mode**: Screen Space - Overlay
- **Reference Resolution**: 1920x1080

```
Canvas
├── HUD (HUDController.cs)
│   ├── HPContainer
│   │   ├── HPBarBackground
│   │   ├── HPBarFill (HealthBar)
│   │   └── HPText
│   └── DynamicCrosshair (DynamicCrosshair.cs)
│       ├── CenterDot
│       ├── TopLine / BottomLine / LeftLine / RightLine
│       └── CrosshairReloadCircle
├── HUDBottomBar (HUDBottomBar.cs)
│   ├── WeaponSlot1 (WeaponSlotUI.cs)
│   │   ├── WeaponIcon
│   │   └── AmmoText
│   ├── WeaponSlot2 (WeaponSlotUI.cs)
│   │   ├── WeaponIcon
│   │   └── AmmoText
│   └── QuickSlotSection
│       └── QuickSlot3~8 (QuickSlot.cs)
└── ReloadUI (ReloadUI.cs)
    └── ReloadBarContainer
        ├── ReloadBarBackground
        └── ReloadBarFill
```

### MapCanvas - Sort Order: 5
- **업데이트 빈도**: M키 토글 시
- **용도**: 전체 맵 UI

```
MapCanvas (MapUI.cs)
└── MapPanel
    ├── MapBackground
    ├── MapContainer
    │   ├── MapImage
    │   ├── PlayerMarker
    │   └── ExtractionMarker
    └── CloseHint
```

### InventoryCanvas - Sort Order: 10
- **업데이트 빈도**: Tab/I 키로 활성화
- **용도**: 인벤토리, 루팅 UI

```
InventoryCanvas (InventoryUI.cs)
└── MainPanel
    ├── InventoryPanel
    │   ├── GridSection
    │   │   └── GridContent (인벤토리 슬롯 30개)
    │   ├── EquipmentSection
    │   │   ├── ArmorSlot
    │   │   ├── PrimaryWeaponSlot
    │   │   └── SecondaryWeaponSlot
    │   └── StatusSection
    │       ├── HealthBar
    │       ├── HydrationIcon
    │       └── EnergyIcon
    ├── LootPanel (루팅 시 표시)
    │   ├── LootTitle
    │   ├── GridSection
    │   │   └── GridContent (루트 아이템 표시)
    │   └── CancelContainer
    └── WeaponSection
        ├── WeaponSlot1
        └── WeaponSlot2
```

### SystemCanvas - Sort Order: 100
- **업데이트 빈도**: 거의 없음
- **용도**: 일시정지, 사망 화면 (시스템 UI)

```
SystemCanvas
├── PauseMenuPanel (PauseMenuUI.cs) - ESC로 토글
│   ├── TitleText ("일시정지")
│   ├── ResumeButton ("게임으로 돌아가기")
│   ├── MainMenuButton ("메인 메뉴로 돌아가기")
│   └── QuitButton ("게임 종료")
└── DeathScreenPanel (DeathScreenUI.cs) - 사망 시 표시
    ├── YouDiedText ("YOU DIED" - 빨간색)
    └── ContinueButton ("계속하기")
```

### Managers Hierarchy (씬 공통)
```
Managers (Empty GameObject)
├── PoolManager (PoolManager.cs)
└── QuickSlotManager (QuickSlotManager.cs)
```

#### QuickSlot 관리 구조
- **QuickSlotManager.cs**: 퀵슬롯 중앙 관리. `slotData[6]`에 ItemData 보관, `inventorySlots`, `hudSlots` 배열로 양쪽 UI 동시 업데이트
- **QuickSlot.cs**: UI 표시만 담당. `UpdateUI(InventoryItem)` 메서드로 Manager에서 호출

