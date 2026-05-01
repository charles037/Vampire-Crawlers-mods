# ComboClick Mod 设计文档

## 概述

一个 BepInEx 模组，在战斗中按下右键时自动打出手牌中可触发连击的卡牌。通过读取游戏自带的连击高亮状态来判断哪些卡可以连击，无需手动点击。

## 行为

| 条件 | 右键行为 |
|------|----------|
| 战斗中，手牌有可连击的卡 | 打出其中费用最低的那张 |
| 战斗中，没有可连击的卡 | 什么都不做 |
| 非战斗中 | 什么都不做 |

## 架构

单文件模组，两个类：

```
mods/ComboClickMod/
├── Plugin.cs              (~100 行)
└── ComboClickMod.csproj
```

### 组件

| 组件 | 职责 |
|------|------|
| `ComboClickPlugin` (BasePlugin) | BepInEx 入口。注册 `ComboClickBehaviour` 为 DontDestroyOnLoad GameObject。 |
| `ComboClickBehaviour` (MonoBehaviour) | `Update()` 轮询：检测右键输入，找可连击卡牌，触发打出。 |

### 数据流

```
Update() 每帧:
  1. Mouse.current.rightButton.wasPressedThisFrame? → 否 → return
  2. GameObject.Find("HandPile") == null? → 是 → return（不在战斗中）
  3. FindObjectOfType<PlayerModel>() == null? → 是 → return
  4. 从 HandPile 获取所有子物体的 CardModel 组件
  5. 筛选：cardModel.ReceivesComboMultiplier == true
  6. 没有? → return
  7. 选 CardConfig.manaCost 最低的那张
  8. playerModel.TryPlayCard(cardModel, isAutoPlay: true)
```

## 关键 API（全部公开）

| API | 来源 | 用途 |
|-----|------|------|
| `Mouse.current.rightButton.wasPressedThisFrame` | Unity Input System | 检测右键点击 |
| `GameObject.Find("HandPile")` | Unity | 战斗检测 + 手牌来源 |
| `FindObjectOfType<PlayerModel>()` | Unity | 获取 PlayerModel 实例 |
| `CardModel.ReceivesComboMultiplier` | Pancake.Models | 判断此卡是否可连击（游戏已高亮） |
| `CardConfig.manaCost` | Pancake.GameConfig | 卡牌费用（多张时选最低费） |
| `PlayerModel.TryPlayCard(CardModel, bool)` | Pancake.Models | 打出卡牌 |

## 错误处理

- 所有调用包在 try/catch 中，模组崩溃不影响游戏
- PlayerModel 或 HandPile 不存在 → 静默跳过
- TryPlayCard 返回 false → 静默忽略

## 测试

- 手动测试：进入战斗，手打一张卡，按右键验证下一张可连击的卡被自动打出
- 边界情况：无手牌、空手牌、费用不够、连击已断
- 验证不按右键时不影响正常游戏

## 参考资料

- 现有模组 `mods/RecipeViewerMod_V1.0/` — BepInEx 插件模式
- 现有模组 `mods/SceneDebugMod/` — 输入轮询模式
- `VampireCrawlersModding.md` — Il2Cpp 反射工具方法
- `Il2CppDumper/dump.cs` — 类型定义
