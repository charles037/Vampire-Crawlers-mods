# Vampire Crawlers 个人 Mod 合集

Vampire Crawlers 的 BepInEx IL2CPP Mod 合集仓库。

---

## Mod 一览

### ComboClickMod（连击自动出牌）

战斗中按右键自动打出可连击的卡牌，按 `` ` `` 键切换模式。

| 模式 | 图标 | 行为 |
|------|------|------|
| 连击 | 飞刀 | 只打高亮可连击卡（普通优先，万能其次） |
| 全自动 | 千刃 | 优先可连击卡，都没有则打最低费卡 |

- 自动跳过消耗卡（一回合打多次会碎的卡也不会自动打出）
- 左下角图标显示当前模式

**文件：** `plugins/ComboClickMod.dll` | 源码 `mods/ComboClickMod/`

---

## 安装方法

> 安装方法参考自 [ZTMYO/VampireCrawlersMods](https://github.com/ZTMYO/VampireCrawlersMods)
>
> 请先自行安装指定版本：`BepInEx-Unity.IL2CPP-*-6.0.0-be.755+3fab71a`。

1. 先下载并解压 BepInEx 压缩包（按你的系统选择）：
   - Windows x64：[BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755+3fab71a.zip](https://builds.bepinex.dev/projects/bepinex_be/755/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755%2B3fab71a.zip)
   - macOS x64：[BepInEx-Unity.IL2CPP-macos-x64-6.0.0-be.755+3fab71a.zip](https://builds.bepinex.dev/projects/bepinex_be/755/BepInEx-Unity.IL2CPP-macos-x64-6.0.0-be.755%2B3fab71a.zip)
   - 其他平台/版本总览：[BepInEx Bleeding Edge 下载总站](https://builds.bepinex.dev/projects/bepinex_be)
2. 解压后你会得到一个外层文件夹（例如 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755+3fab71a`）。
3. 打开这个外层文件夹，把**里面的所有文件和文件夹**复制到游戏根目录（不要把这个外层文件夹本身直接丢进游戏目录）。
4. 正确结果通常是游戏根目录同时有：`BepInEx/`、`dotnet/`、`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`。
5. 将本仓库 `plugins/` 目录下的 `*.dll` 复制到游戏目录的 `BepInEx/plugins/` 中。
6. 启动游戏进入战斗即可生效。

> 首次启动说明：第一次启动游戏时，BepInEx IL2CPP 会自动下载 Unity 基础库并生成互操作文件，可能需要等待一段时间（通常几十秒到几分钟）。期间日志出现 `Downloading unity base libraries`、`Extracting unity base libraries`、`Running Cpp2IL`、`Creating application model` 都是正常现象，请耐心等待完成，不要中途强退。

---

## 项目结构

- `plugins/`：已编译的 Mod 插件（`ComboClickMod.dll`）。
- `mods/`：Mod C# 源码与项目文件。

---

## 免责声明

- 本项目仅用于单机环境下的界面与交互优化学习，不用于任何联机对抗或破坏公平性的用途。
- 本项目不提供绕过反作弊、破解付费内容、篡改联机数据等功能。
- 请遵守游戏 EULA 与相关平台规则；因使用本项目产生的风险与后果由使用者自行承担。
