# ModSettings 翻译初始化时序修复

## 状态

- 日期：2026-07-31
- 静态修复：已完成
- 运行副本同步：已完成
- 游戏验收：等待用户手动复测

## 症状

RimWorld 加载模组类时出现：

```text
No active language! Cannot translate from key GD.Settings.CacheInvalidated.
LoadedModManager.CreateModClasses -> GD_Mod..ctor -> GD_SettingsAccess.ApplyChanged -> Translator.Translate
```

## 根因

`GD_Mod` 构造函数在 `CreateModClasses` 阶段强制调用 `GD_SettingsAccess.ApplyChanged(force: true)`。本地化迁移后，该方法会翻译缓存失效日志；但这一阶段 `LanguageDatabase.activeLanguage` 尚未建立，因此任何 `.Translate()` 都会报错。

## 修复

- 构造阶段改为 `ApplyChanged(force: true, logChange: false)`，继续初始化指纹并清空缓存，但不输出玩家语言日志。
- 日志分支同时检查 `LanguageDatabase.activeLanguage != null`，防止未来其他早期调用再次触发翻译。
- `GD_WorldGenePool.ClearCache(logMessage: false)` 同步关闭底层技术日志，保证构造阶段完全静默；Debug Action 和正常设置变更仍使用默认日志行为。
- 设置窗口内的真实设置变更仍使用默认 `logChange: true`，因此活动语言建立后日志行为不变。

## 静态证据

- RimWorld `LanguageDatabase` 源码确认 `activeLanguage` 为公开字段，`Clear()` 会置空，`InitAllMetadata()` 才会赋值。
- strict validate：0 error，0 warning。
- Release build：0 error，0 warning。
- 修复前 DLL SHA-256：`7BB96BEFE56A419E21FFB7F394756EE169E37910C88C90A42CF931C815D49DE4`。
- 首轮修复中间 DLL SHA-256：`55438EDF1CCE75BC05E6B5207CB04B65E6E670384EA0F48737B5A353A188C07D`；追加构造阶段完全静默后已被最终产物替代。
- 最终开发/运行 DLL SHA-256：`214628C3C3CB316422631E253B3DD6C33ACC8D21F067B1911906151FCAC503D5`。
- `sync-verify`：9 个必需运行文件全部一致，运行副本污染扫描通过。
- 最终同步 manifest：`D:\RimWorldModding\迁移记录\sync-manifests\yyyyy_GeneticDiversity\sync-yyyyy_GeneticDiversity-20260731T143922.419682Z.json`。

## 待验收

由用户手动重启 RimWorld；确认启动阶段不再出现该红字，并分别检查英文与简体中文 ModSettings。没有修复后新会话前，不得把静态校验写成游戏内通过。
