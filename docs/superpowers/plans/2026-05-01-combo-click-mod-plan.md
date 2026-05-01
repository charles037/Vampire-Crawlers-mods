# ComboClick Mod 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现一个 BepInEx 模组，战斗中按右键自动打出可连击的卡牌（读取游戏自带的高亮状态）。

**Architecture:** 单文件 `Plugin.cs`，`ComboClickPlugin` 注册 `ComboClickBehaviour` 为 DontDestroyOnLoad GameObject，`Update()` 内轮询右击 → 遍历 HandPile 子卡 → 筛选 `ReceivesComboMultiplier == true` → 选最低费 → `PlayerModel.TryPlayCard()`。

**Tech Stack:** C# / .NET 6.0 / BepInEx 6 IL2CPP / Unity Input System / Pancake interop DLL

---

### Task 1: 创建项目结构和 .csproj

**Files:**
- Create: `mods/ComboClickMod/ComboClickMod.csproj`
- Create: `mods/ComboClickMod/Plugin.cs`（空骨架）

- [ ] **Step 1: 创建 .csproj 文件**

内容如下（精简版，去掉 UI 相关引用，只保留必需的）：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>ComboClickMod</AssemblyName>
    <RootNamespace>ComboClickMod</RootNamespace>
    <Version>1.0.0</Version>
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
    <Reference Include="UnityEngine"><HintPath>..\..\BepInEx\interop\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>..\..\BepInEx\interop\UnityEngine.CoreModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.InputModule"><HintPath>..\..\BepInEx\interop\UnityEngine.InputModule.dll</HintPath></Reference>
    <Reference Include="Unity.InputSystem"><HintPath>..\..\BepInEx\interop\Unity.InputSystem.dll</HintPath></Reference>
    <Reference Include="Il2Cppmscorlib"><HintPath>..\..\BepInEx\interop\Il2Cppmscorlib.dll</HintPath></Reference>
    <Reference Include="Il2CppSystem"><HintPath>..\..\BepInEx\interop\Il2CppSystem.dll</HintPath></Reference>
    <Reference Include="Il2CppSystem.Core"><HintPath>..\..\BepInEx\interop\Il2CppSystem.Core.dll</HintPath></Reference>
    <Reference Include="Pancake"><HintPath>..\..\BepInEx\interop\Pancake.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 创建 Plugin.cs 骨架**

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ComboClickMod;

[BepInPlugin("com.comboclick.mod", "Combo Click Mod", "1.0.0")]
public class ComboClickPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("ComboClickMod loading...");
    }

    public override bool Unload() => true;
}
```

- [ ] **Step 3: 编译验证项目能通过**

```powershell
dotnet build "mods/ComboClickMod/ComboClickMod.csproj" -c Release
```

预期：Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add mods/ComboClickMod/
git commit -m "feat: scaffold ComboClickMod project"
```

---

### Task 2: 实现 ComboClickBehaviour 主逻辑

**Files:**
- Modify: `mods/ComboClickMod/Plugin.cs`

- [ ] **Step 1: 添加 ComboClickBehaviour 类和 Load 注册**

编辑 `mods/ComboClickMod/Plugin.cs`，替换为完整实现：

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.GameConfig;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ComboClickMod;

[BepInPlugin("com.comboclick.mod", "Combo Click Mod", "1.0.0")]
public class ComboClickPlugin : BasePlugin
{
    public override void Load()
    {
        ClassInjector.RegisterTypeInIl2Cpp(typeof(ComboClickBehaviour));
        var go = new GameObject("ComboClickBehaviourObj");
        go.AddComponent<ComboClickBehaviour>();
        Object.DontDestroyOnLoad(go);
        Log.LogInfo("ComboClickMod loaded. Right-click in combat to auto-play combo cards.");
    }

    public override bool Unload() => true;
}

public class ComboClickBehaviour : MonoBehaviour
{
    public ComboClickBehaviour(System.IntPtr ptr) : base(ptr) { }

    private void Update()
    {
        try
        {
            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
                return;

            var handPile = GameObject.Find("HandPile");
            if (handPile == null) return;

            var playerModel = FindObjectOfType<PlayerModel>();
            if (playerModel == null) return;

            CardModel bestCard = null;
            int bestCost = int.MaxValue;

            var components = handPile.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in components)
            {
                if (mb == null) continue;
                if (mb is not CardModel card) continue;

                var config = card.CardConfig;
                if (config == null) continue;

                if (!card.ReceivesComboMultiplier) continue;

                var cost = config.manaCost;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestCard = card;
                }
            }

            if (bestCard != null)
                playerModel.TryPlayCard(bestCard, true);
        }
        catch { }
    }
}
```

- [ ] **Step 2: 编译**

```powershell
dotnet build "mods/ComboClickMod/ComboClickMod.csproj" -c Release
```

预期：Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add mods/ComboClickMod/Plugin.cs
git commit -m "feat: implement right-click combo card auto-play"
```

---

### Task 3: 部署并测试

**Files:**
- Copy: `mods/ComboClickMod/bin/Release/net6.0/ComboClickMod.dll` → `BepInEx/plugins/`

- [ ] **Step 1: 编译 Release 版本**

```powershell
dotnet build "mods/ComboClickMod/ComboClickMod.csproj" -c Release
```

- [ ] **Step 2: 清除 BepInEx 缓存并复制 DLL**

```powershell
Remove-Item -Recurse -Force "BepInEx\cache" -ErrorAction SilentlyContinue
Copy-Item "mods\ComboClickMod\bin\Release\net6.0\ComboClickMod.dll" "BepInEx\plugins\ComboClickMod.dll" -Force
```

- [ ] **Step 3: 验证 DLL 已就位**

```powershell
Test-Path "BepInEx\plugins\ComboClickMod.dll"
```

预期：True

- [ ] **Step 4: 启动游戏手动测试**

测试步骤：
1. 启动游戏，检查 BepInEx 控制台是否输出 `ComboClickMod loaded.`
2. 进入一场战斗
3. **手动**打出第一张卡
4. 按右键 → 验证下一张可连击的卡被自动打出
5. 连续按右键 → 验证连击连续性
6. 连击断了（无高亮卡）按右键 → 验证什么都不发生
7. 非战斗场景按右键 → 验证什么都不发生

- [ ] **Step 5: 检查 BepInEx 日志无报错**

```powershell
Get-Content "BepInEx\LogOutput.log" -Tail 30
```

- [ ] **Step 6: Commit**

```bash
git add BepInEx/plugins/ComboClickMod.dll
git commit -m "chore: deploy ComboClickMod to plugins"
```
