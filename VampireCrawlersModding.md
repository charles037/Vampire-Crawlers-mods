# Vampire Crawlers 模组开发参考

## 1. 游戏基本信息

| 项 | 值 |
|----|-----|
| 引擎 | Unity 6000.0.62f1 |
| 脚本后端 | IL2CPP |
| 框架 | Pancake (`Nosebleed.Pancake.*`) |
| 模组框架 | BepInEx 6.0.0-be.755 (IL2CPP) |
| .NET 运行时 | .NET 6.0.7 |

## 2. BepInEx 环境

### 目录结构
```
game_root/
├── BepInEx/
│   ├── cache/              # 每次修改 DLL 后删除此目录
│   ├── interop/            # BepInEx 自动生成的 IL2CPP 互操作 DLL
│   │   ├── Pancake.dll              # Pancake 框架类型
│   │   ├── Assembly-CSharp.dll      # 游戏主程序集
│   │   ├── UnityEngine*.dll         # Unity 引擎模块
│   │   ├── Il2Cpp*.dll              # IL2CPP 运行时
│   │   └── Sirenix.*.dll            # Sirenix Odin 序列化
│   ├── plugins/            # 编译好的 mod DLL 放这里
│   │   └── Recipes.txt     # 配方数据（自动生成）
│   └── LogOutput.log       # BepInEx 日志
└── mods/                   # 源代码放这里
    └── RecipeViewerMod/
        ├── Plugin.cs
        ├── RecipePanel.cs
        ├── RecipeData.cs
        └── RecipeViewerMod.csproj
```

### .csproj 模板
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <LangVersion>latest</LangVersion>
    <RestoreAdditionalProjectSources>
      https://api.nuget.org/v3/index.json;
      https://nuget.bepinex.dev/v3/index.json;
      https://nuget.samboy.dev/v3/index.json
    </RestoreAdditionalProjectSources>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BepInEx.Unity.IL2CPP" Version="6.0.0-be.*" IncludeAssets="compile" />
    <PackageReference Include="BepInEx.PluginInfoProps" Version="2.*" />
  </ItemGroup>
  <ItemGroup>
    <!-- 基础 Unity 引用 -->
    <Reference Include="UnityEngine"><HintPath>..\..\BepInEx\interop\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>..\..\BepInEx\interop\UnityEngine.CoreModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.UI"><HintPath>..\..\BepInEx\interop\UnityEngine.UI.dll</HintPath></Reference>
    <Reference Include="UnityEngine.UIModule"><HintPath>..\..\BepInEx\interop\UnityEngine.UIModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.TextRenderingModule"><HintPath>..\..\BepInEx\interop\UnityEngine.TextRenderingModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.InputModule"><HintPath>..\..\BepInEx\interop\UnityEngine.InputModule.dll</HintPath></Reference>
    <Reference Include="Unity.InputSystem"><HintPath>..\..\BepInEx\interop\Unity.InputSystem.dll</HintPath></Reference>
    <Reference Include="Il2Cppmscorlib"><HintPath>..\..\BepInEx\interop\Il2Cppmscorlib.dll</HintPath></Reference>
    <Reference Include="Il2CppSystem"><HintPath>..\..\BepInEx\interop\Il2CppSystem.dll</HintPath></Reference>
    <Reference Include="Il2CppSystem.Core"><HintPath>..\..\BepInEx\interop\Il2CppSystem.Core.dll</HintPath></Reference>
    <!-- Pancake 框架（需要 Harmony 钩子时添加） -->
    <Reference Include="Pancake"><HintPath>..\..\BepInEx\interop\Pancake.dll</HintPath></Reference>
    <!-- 如需直接访问 CardConfig 等类型，还需要 Sirenix -->
    <Reference Include="Sirenix.Serialization"><HintPath>..\..\BepInEx\interop\Sirenix.Serialization.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```

### 插件基本结构（BepInEx IL2CPP）
```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

[BepInPlugin("com.example.mod", "Mod Name", "1.0.0")]
public class MyPlugin : BasePlugin
{
    public override void Load()
    {
        // 注册 MonoBehaviour 子类
        ClassInjector.RegisterTypeInIl2Cpp(typeof(MyBehaviour));
        var go = new GameObject("MyObj");
        go.AddComponent<MyBehaviour>();
        Object.DontDestroyOnLoad(go);
    }
}

// MonoBehaviour 必须定义此构造函数供 IL2CPP 使用
public class MyBehaviour : MonoBehaviour
{
    public MyBehaviour(System.IntPtr ptr) : base(ptr) { }
}
```

**每次修改 DLL 后必须删除 `BepInEx/cache/` 目录。**

## 3. Pancake 框架关键类型

所有类型在 `BepInEx/interop/Pancake.dll` 中。

### 数据层
| 类型 | 命名空间 | 说明 |
|------|----------|------|
| `CardConfig` | `Nosebleed.Pancake.GameConfig` | 卡牌配置（ScriptableObject） |
| `CardGroup` | `Nosebleed.Pancake.GameConfig` | 卡牌组/类别 |
| `CardRewardInfo` | `Nosebleed.Pancake` | 奖励配置 |
| `CardDatabase` | - | 卡牌数据库（ScriptableObject，name="CardDatabase"） |

### 视图层
| 类型 | 命名空间 | 说明 |
|------|----------|------|
| `CardChoiceView` | `Nosebleed.Pancake.View` | 升级选卡界面（MonoBehaviour） |
| `CardView` | `Nosebleed.Pancake.View` | 单张卡牌显示 |
| `CardLevelUpView` | `Nosebleed.Pancake.View` | 卡牌升级视图 |
| `CardSelectionCardView` | `Nosebleed.Pancake.View` | 可选卡牌视图 |
| `ScrollableCardSelectionView` | `Nosebleed.Pancake.View` | 可滚动卡牌选择视图 |
| `EvoLevelUpView` | `Nosebleed.Pancake.View` | 进化升级视图 |
| `EvoTableEventView` | `Nosebleed.Pancake.View` | 进化台事件视图 |
| `CrystalPickupView` | `Nosebleed.Pancake.View` | 水晶拾取事件 |
| `GemChestEventView` | `Nosebleed.Pancake.View` | 宝石宝箱事件 |
| `EnemyCardThrowingEventView` | `Nosebleed.Pancake.View` | 敌人卡牌掉落事件 |
| `DarkanaChoiceEventView` | `Nosebleed.Pancake.View` | Darkana 选择事件 |
| `PassiveEventModal` | `Nosebleed.Pancake.Modal` | 被动事件模态框 |

### 模型层
| 类型 | 命名空间 | 说明 |
|------|----------|------|
| `PlayerModel` | `Nosebleed.Pancake.Models` | 玩家模型 |
| `EnemyModel` | `Nosebleed.Pancake.Models` | 敌人模型 |
| `EnemyEncounterModel` | `Nosebleed.Pancake.Models` | 遭遇战模型 |
| `CardModel` | `Nosebleed.Pancake.Models` | 卡牌模型 |
| `ICardModel` | `Nosebleed.Pancake.Models` | 卡牌模型接口 |
| `ChoosableCard` | `Nosebleed.Pancake.GameLogic` | 可选卡牌 |

### 有用的 Harmony 钩子目标
- `PlayerModel.Update` — 每帧调用，可用 `IsInEncounter` 检测战斗状态
- `PlayerModel.OnEncounterStarted` — 进入战斗
- `EnemyEncounterModel.EnableEncounter` — 遭遇战启用
- `EnemyEncounterModel.OnEncounterEnded` — 遭遇战结束
- `EnemyModel.Start` — 敌人生成
- `EnemyModel.OnEnemyAddedToGroup` — 敌人加入组
- `CardChoiceView.OnEnable` — 选卡界面打开
- `CardChoiceView.OnDisable` — 选卡界面关闭
- `PassiveEventModal.SetPassiveEventView(PassiveEventView)` — 被动事件设置

## 4. 内存布局（CardConfig / CardGroup）

通过 Il2CppDumper dump.cs 分析。

### CardGroup 字段（继承自 SerializedScriptableObject → ScriptableObject → UnityEngine.Object）
| 偏移 | 字段 | 类型 |
|------|------|------|
| +0x60 | `groupName` | string (Il2Cpp pointer) |
| +0x68 | `icon` | Sprite (pointer) |
| +0x70 | `_isWeapon` | bool |
| +0x78 | `_evolvedCardConfig` | CardConfig pointer |

### CardConfig 字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string (property) | 卡牌中文名 |
| `cardGroup` | CardGroup* | 卡牌组 |
| `EvolutionComponents` | List\<CardGroup\> | 合成材料列表（isEvolvedCard=true 的卡才有值） |
| `sprites` | Sprite[] | 卡牌多级图标数组 |
| `cardType` | CardType* | 卡牌类型（红/黄/紫） |
| `isEvolvedCard` | bool | 是否进化卡 |
| `manaCost` | int | 法力消耗 |

### 重要发现
- **CardGroup.icon** 是组的共享图标：红/黄卡是正确图标，紫卡（防御类）是通用绿叶
- **CardConfig.sprites[0]** 是卡牌自有图标，所有类型都正确
- **同一中文名可能对应多个 CardConfig**（如"防御"有基础版和进化版），它们属于不同 CardGroup
- **_spriteCache 用中文名做 key 时，后提取的会覆盖前面的**。修复：`!ContainsKey` 检查

## 5. Il2Cpp 反射工具方法

### 读属性值
```csharp
static Il2CppSystem.Object GetProp(Il2CppSystem.Object obj, string name)
    => obj.GetIl2CppType().GetProperty(name)?.GetValue(obj);
```

### 读字段值
```csharp
static Il2CppSystem.Object GetIl2CppField(Il2CppSystem.Object obj, string name)
    => obj.GetIl2CppType().GetField(name,
        Il2CppSystem.Reflection.BindingFlags.Public |
        Il2CppSystem.Reflection.BindingFlags.NonPublic |
        Il2CppSystem.Reflection.BindingFlags.Instance)?.GetValue(obj);
```

### 枚举 Il2Cpp List
```csharp
static List<Il2CppSystem.Object> ReadIl2CppList(Il2CppSystem.Object listObj)
{
    var r = new List<Il2CppSystem.Object>();
    if (listObj == null) return r;
    var en = listObj.GetIl2CppType().GetMethod("GetEnumerator").Invoke(listObj, null);
    var et = en.GetIl2CppType();
    while (Unbox<bool>(et.GetMethod("MoveNext").Invoke(en, null)))
        r.Add(et.GetProperty("Current").GetValue(en));
    return r;
}

static T Unbox<T>(Il2CppSystem.Object v) where T : unmanaged => v.Unbox<T>();
```

### 读 Il2Cpp 字符串
```csharp
var managed = Il2CppInterop.Runtime.IL2CPP.Il2CppStringToManaged(obj.Pointer);
```

### 读 Sprite（CardGroup +0x68 偏移）
```csharp
static unsafe Sprite ReadSprite(Il2CppSystem.Object obj)
{
    if (obj == null) return null;
    long ptr = *(long*)(obj.Pointer.ToInt64() + 0x68);
    if (ptr == 0) return null;
    return new Sprite((System.IntPtr)ptr);
}
```

### 读 CardConfig.sprites（需要 Sirenix.Serialization 引用）
```csharp
try
{
    var cfg = new CardConfig(card.Pointer);
    var sprites = cfg.sprites;
    if (sprites != null && sprites.Length > 0)
        return sprites[0];
}
catch { }
```

## 6. 卡组检测

### GameObject 查找
游戏中卡牌存在于以下 GameObject 的子物体中：
- `HandPile` — 手牌堆
- `DrawPile` — 抽牌堆
- `DiscardPile` — 弃牌堆

### 检测方法
```csharp
HashSet<string> DetectOwnedCards()
{
    var owned = new HashSet<string>();
    foreach (var name in new[] { "HandPile", "DrawPile", "DiscardPile" })
    {
        var pile = GameObject.Find(name);
        if (pile == null) continue;
        foreach (var mb in pile.GetComponentsInChildren<MonoBehaviour>(true))
        {
            // 方式1: 搜索 CardModel 组件（部分卡类型）
            if (mb.GetIl2CppType().FullName.Contains("CardModel"))
            {
                var config = GetProp(mb, "CardConfig");
                // 读 Name...
            }
            // 方式2: 搜索 CardView 组件并通过 _cardModel 字段获取
            if (mb.GetIl2CppType().FullName.Contains("CardView"))
            {
                var field = mb.GetIl2CppType().GetField("_cardModel", ...);
                var cardModel = field.GetValue(mb);
                var config = GetProp(cardModel, "CardConfig");
                // 读 Name...
            }
        }
    }
    return owned;
}
```

### 模糊匹配（防御卡名称不完整匹配）
配方中的输入名为基础名（如"防御"），但拥有的卡可能叫"黄金防御"。用模糊匹配：
```csharp
_ownedCards.Any(c => c.Contains(recipeName) || recipeName.Contains(c))
```

## 7. 配方提取

```csharp
// 遍历所有 ScriptableObject，找到 CardDatabase
foreach (var so in Resources.FindObjectsOfTypeAll<ScriptableObject>())
{
    if (so.name != "CardDatabase") continue;
    var cards = ReadIl2CppList(GetProp(so, "Assets"));
    
    // 第一步：建立 groupNameMap (CardGroup → 中文名)
    foreach (var card in cards)
    {
        var group = card.GetIl2CppType().GetField("cardGroup")?.GetValue(card);
        if (group == null) continue;
        var cn = ReadCardName(card);  // CardConfig.Name
        groupNameMap[group.Pointer] = cn;
    }
    
    // 第二步：提取合成配方
    foreach (var card in cards)
    {
        var comps = ReadIl2CppList(GetProp(card, "EvolutionComponents"));
        if (comps.Count == 0) continue;  // 非进化卡，跳过
        // comps[n].Pointer 对应 CardGroup pointer
        // 从 groupNameMap 查出中文名
        // 合成输出 = ReadCardName(card)
    }
}
```

**注意**：只有 `isEvolvedCard=true` 的卡才有 `EvolutionComponents`（不是 `HasEvolution` 的卡）。

**配方格式**：`Recipes.txt`
```
输入A|输入B|输出        # 2 材料
输入A|输入B|输入C|输出   # 3 材料
```

## 8. 进入关卡检测

### 轮询方式（推荐，稳定）
```csharp
// 每 2 秒检查
var inDungeon = GameObject.Find("HandPile") != null || GameObject.Find("DrawPile") != null;
if (inDungeon && !_wasInDungeon) { /* 进入关卡 */ }
```

### Harmony 钩子方式
```csharp
[HarmonyPatch(typeof(PlayerModel), "OnEncounterStarted")]
public static void Postfix() { /* 进入战斗 */ }
```

## 9. UI 面板

### 创建 Canvas（ScreenSpaceOverlay）
```csharp
var go = new GameObject("Canvas");
var cv = go.AddComponent<Canvas>();
cv.renderMode = RenderMode.ScreenSpaceOverlay;
cv.sortingOrder = 999;
go.AddComponent<CanvasScaler>();
go.AddComponent<GraphicRaycaster>();
Object.DontDestroyOnLoad(go);
```

### 立即销毁（避免残留）
```csharp
Object.DestroyImmediate(obj);
// NOT Object.Destroy(obj); — 会延迟到帧末销毁，导致旧面板残留一帧
```

### 文字颜色
| 用途 | Color |
|------|-------|
| 已拥有卡（红底） | `(0.55, 0.06, 0.06, 0.85)` |
| 置顶配方（黄底） | `(0.65, 0.6, 0.25, 1.0)` |
| 面板背景 | `(0.05, 0.05, 0.05, 0.92)` |

## 10. 反编译工具

### Il2CppDumper
- 输出 `dump.cs`（42MB）、`script.json`、`il2cpp.h`
- 生成 DummyDll 程序集（用于编译时引用，不能直接运行时用，有 Sirenix 冲突）
- 路径：`C:\Users\Administrator\AppData\Local\Temp\Il2CppDumper\`

### ilspycmd（反编译 .NET DLL）
```bash
dotnet tool install -g ilspycmd
ilspycmd input.dll -o output_dir
```

### UnityPy（提取游戏资源）
- 用于提取纹理等资源文件

## 11. 已知陷阱

| 问题 | 解决 |
|------|------|
| Pancake DummyDll 引用导致 Sirenix 冲突 | 使用 BepInEx interop 的 Pancake.dll，无需 DummyDll |
| `GetComponentsInChildren(true)` 包含非激活模板 | 用 `(false)` 只搜激活的，或检查 `activeInHierarchy` |
| Il2CppString.TryCast 报错 | 改用 `Il2CppStringToManaged(v.Pointer)` |
| IL2CPP 数组无法通过 C# 索引访问 | 用枚举器 `GetEnumerator`/`MoveNext`/`Current` |
| 直接读写 CardConfig 字段需要 Sirenix 引用 | 添加 `Sirenix.Serialization.dll` 引用，或用 Il2Cpp 反射 |
| `Object.Destroy` 不立即生效 | 用 `Object.DestroyImmediate` |
| `SceneManager.sceneLoaded` 在 IL2CPP 不可用 | 用轮询 `GameObject.Find("HandPile")` 检测场景切换 |
| 修改 DLL 后不生效 | 删除 `BepInEx/cache/` |
| MonoBehaviour 缺少 `(IntPtr)` 构造函数 | 必须定义 `public MyClass(IntPtr ptr) : base(ptr) { }` |
| Harmony `PatchAll()` 需要 `new Harmony("id")` | BepInEx IL2CPP 中 Harmony 随 BepInEx 传递引入，无需额外 NuGet |
| 同名卡多个 CardConfig 导致图标覆盖 | `_spriteCache` 用 `ContainsKey` 检查，保留第一个 |

## 12. 当前版本功能（v1.0）

- 配方提取：从 CardDatabase 自动提取 17 条合成配方
- 卡组检测：轮询 HandPile/DrawPile/DiscardPile 中的 CardModel 组件
- 红底高亮：拥有材料卡时红色标识
- 选卡置顶：进入 CardChoiceView 选卡界面时，涉及配方置顶 + 黄底
- 模糊匹配：防御类卡牌支持名称包含匹配
- 进入关卡自动检测 + 重试（最多 2 秒 × 15 次）
- Tab 切换面板显示/隐藏
- 图标显示：CardGroup.icon（+0x68 偏移）
