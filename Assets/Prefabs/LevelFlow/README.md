# LevelFlow（通关 / 传送门流程）

本目录包含：**脚本**（`Scripts/`）、**三个预制体**、**音效**（`Audio/`），以及本说明。用于在任意关卡末尾放置传送门、弹出胜利界面（收集数、用时、再玩 / 下一关 / 选关）。

---

## 依赖（工程里已有则不用动）

- `GameManager`（`Assets/GameManager.cs`）：全屏 Fade 在透明时不挡 UI 点击；切场景时重置复活点等。
- `CollectibleProgress` / `Collectible2D`（`Assets/Collectibles/`）：收集统计。
- `PlayerController`：胜利时禁用操作。
- 场景中有 **Tag = `Hero`** 的玩家。
- **EventSystem**（建议带 **Input System UI Input Module**，与本项目输入方式一致）。

---

## 预制体说明

| 预制体 | 作用 |
|--------|------|
| **LevelRunTracker** | 每关放一个。记录进入本关后的用时（不受 `timeScale` 影响）。 |
| **EndPortal2D** | 终点传送门：圆形触发器 + 简单精灵；`Hero` 进入时**播放一次**终点音效（默认 `Audio/track_start.wav`），再打开胜利界面。可在 Inspector 换 `Portal Enter Clip` / 音量。 |
| **VictoryCanvas** | 胜利 UI：全屏暗底 + 面板 + Congratulations + 收集/用时 + 三个按钮（再玩一次 / 下一关 / 进入选关）。 |

脚本位置：`Scripts/LevelRunTracker.cs`、`EndPortal2D.cs`、`LevelVictoryUI.cs`（**GUID 未改**，与旧路径 `LevelEnd` 兼容，已挂好的场景引用不会丢）。

---

## 应用到新关卡（最短步骤）

1. **File → Build Settings**  
   把本关、**下一关场景**、菜单场景（如 `0_Menu`）全部加入列表并勾选，否则 `LoadScene` 会报错。

2. 打开目标关卡场景，拖入预制体：  
   - **VictoryCanvas**（可放在 Hierarchy 根下，独立 Canvas）  
   - **LevelRunTracker**（任意空位即可）  
   - **EndPortal2D** 放到关卡终点，调整 **Transform** 与 **CircleCollider2D** 半径。

3. 选中 **VictoryCanvas** 根物体上的 **LevelVictoryUI**：  
   - **Next Scene Name**：下一关的**场景文件名**（无 `.unity`），与 Build Settings 一致，例如 `2_Yoru`。留空则隐藏「下一关」按钮。  
   - **Level Select Scene Name**：默认 `0_Menu`，可按项目改。  

4. 选中 **LevelRunTracker** 实例：若本关 **Collectible2D** 的 `levelId` 不是当前场景名，在 **Level Id Override** 里填与收集物一致的字符串；否则可留空。

5. **EndPortal2D** 的 **Victory UI** 可留空，会**自动查找** `LevelVictoryUI`。场景里 UI 很多时建议手动拖引用。

6. 运行测试：走进传送门 → **听到音效** → 界面与按钮正常 → 再玩 / 下一关 / 选关。

---

## 终点音效（`Audio/`）

- 默认使用 **`Audio/track_start.wav`**，已在 **EndPortal2D** 预制体上引用。
- 通关时会在 **`Time.timeScale` 归零前**按**真实时间**等待约「clip 长度 + 0.05s」，避免 Unity 在暂停游戏时间后把声音卡掉；期间角色已被禁用操作。
- 换音效：把新 `.wav` 放进 `Audio/`（或任意路径），在 **EndPortal2D** 上把 **Portal Enter Clip** 指到新资源即可。

---

## 给美术 / 策划的变体用法

- **只改下一关、不改母预制体**：在 Project 里对 **VictoryCanvas** 右键 **Prefab → Create Variant**，每个变体只改 **Next Scene Name**。  
- **换传送门外观**：改 **EndPortal2D** 的 Sprite / 材质，或替换为 **BoxCollider2D**（保持 **Is Trigger** 即可）。

---

## 常见问题

- **按钮点了没反应**：通常是 **GameManager** 的 **FadeImage** 全屏挡射线；请使用工程内已更新的 `GameManager.SyncFadeRaycastTarget` 逻辑，或保证胜利 Canvas **Sorting Order** 高于 Fade（预制体默认 400）。  
- **下一关加载失败**：场景未加入 **Build Settings**。  
- **收集数不对**：`LevelRunTracker` 的 levelId 与 **Collectible2D** 的 `levelId` 不一致，或该关收集物尚未在场景里跑过一次以写入总数。

---

## 发给其他仓库 / 队友

可 **Export Package**：勾选整个 `Assets/Prefabs/LevelFlow` 以及依赖的 `Collectibles`、`GameManager.cs` 等；对方 **Import** 后按上文步骤摆预制体并配置 Build Settings。
