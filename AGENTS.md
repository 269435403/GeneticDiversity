# 基因多样性开发约束（所有后续 Agent 必读）

> 最后更新：2026-07-30｜阶段 1～6 已完整验收并收口；阶段 6 收口后修复了主菜单 ModSettings 访问 `Find.Selector` 的生命周期异常，修复 DLL 已同步，等待用户手动复测。

## 不可变核心理念

本模组模拟的是一个 RimWorld 世界在数百、数千年运行中的基因传承与族群扩散，不是从已加载 Def 中无差别随机塞基因。

任何新增或修改随机基因逻辑时，必须遵守：

1. 最高优先：当前存档实际 Pawn 的 Endogenes，并按出现频率形成权重；
2. 原版 Human/Xenotype 与 HAR Race/Xenotype 地位相同；
3. 同阵营同 Race ×4、同 Race ×2、其他已出现 Race 的标准基因 ×1；
4. 自定义 geneClass 与身体/外观结构基因默认只在同 Race 实际观察或明确声明时传播；
5. 只有世界系谱池为空、候选不兼容或机制明确属于低概率突变时，才使用 Def 回退/突变池；
6. 不删除核心基因、不替换 Xenotype、不转换 Xenogenes、不放宽冲突/前置/代谢/Race 安全；
7. 新差异优先成为 Endogene，并交给原版生育继续传承。

## 当前实现边界

- 阶段 1：新生成非新生儿 Pawn 的 0～3 位点，90% 世界常见/10% 突变；
- 阶段 2：仅原版 Human 真正出生时的一次 10% 单基因突变，不改父母遗传/GeneSet；
- 阶段 3：已实现 HAR 通用生成兼容、当前世界 HAR Endogenes、4/2/1 权重、CanHaveGene/CanUseXenotype、raceGenes/Xenotype 只读回退；
- 阶段 3 不扩展 HAR 出生突变，不接入阵营种族多样性设置/API，不做已知 HAR 基因模组精确白名单；
- 阶段 4 已实现 Kiiro、Milira、Wolfein、Ratkin Gene Expanded、OA Ratkin 与 Epona/Milira 混血精确兼容；用户日志、清理后 Release 构建、白名单同步、按钮字符串扫描与最终哈希均已完成，最终 DLL SHA-256 为 `88418CE0633359AEE7F585DB901D0237E4C43D776FF0062A96349DFB2793B6E0`。`GD_Phase4AutomatedTests.cs` 已删除，不得恢复。
- 阶段 4.5 已完整验收并清理测试入口，为十一个实际基因来源补齐精确规则：原十个来源 Nivarian、Ratkin、OA、Kiiro 橘猫/缅因/暹罗/布偶、Dragonian、Axolotl、Yuran，以及 `[SRC]Miho` 的硬依赖基础模组 `miho.fortifiedoutremer`。`[SRC]Miho` 自身不定义 GeneDef；基础 Miho 在 1.6 活动根中定义 12 个核心 GeneDef、5 个核心可遗传 Xenotype，并在 Odyssey 条件根中追加 3 个 GeneDef、1 个可遗传 Xenotype。`GD_Phase45AutomatedTests.cs` 已删除，清理后 Release 构建与运行副本同步通过，不得恢复。Moosesian 的基因文件当前不在 RimWorld 1.6 实际加载根下，因此不登记。
- 阶段 5 已完整完成：`GD_FrdAdapter.cs` 只通过可选反射读取 FRD 公共只读服务/设置；目标 Race 无世界样本时才加入低权重 Xenotype 基因先验，不重选 Race/PawnKind/Xenotype；FCD 保持独立。原五项联动测试与随机基因生成回归均由 2026-07-30 同轮日志判定通过；20 个临时 Pawn 产生 24 个随机位点并成功新增 24 个基因，异常 0。主池全部不兼容时才进入受限恢复池，全部安全检查不放宽。`GD_Phase5AutomatedTests.cs` 已删除，最终 DLL SHA-256 为 `83FA51F6F9B9575F863B1D423236EA980533D857CC90CEA7BD66288FCFDA82A3`，不得恢复阶段 5 临时按钮。
- 阶段 6 尚未开始；后续只有在用户明确要求进入阶段 6 时才实施。

## HAR 永久规则

- HAR 必须是可选软兼容，不得在 csproj 引用 AlienRace.dll；
- HAR 未安装或适配失败时，原版 Human 保留，非 Human 保守跳过；
- 不得调用 AlienChanceEntry.Select/Approved 构建缓存；
- HAR CanHaveGene 预检与 AddGene Prefix 是最终裁决，不得绕过；
- 不以母亲 Race 猜测 HAR 新生儿最终 Race。

修改前同时阅读：`模组文档/核心设计公约.md`、`模组文档/模组修改注意事项.md`、当前阶段测试/交接文档。

## 测试交付规则

- 所有交给普通玩家的游戏内测试必须有准确中文的开发者操作按钮。
- 按钮必须自动构造前置、执行、统计并输出“通过/失败/未执行”；禁止要求玩家手工编辑人物、批量生成、人工计数或计算。
- 测试文档必须写出原版“开发人员模式”“开关Debug操作菜单”的进入方法、完整按钮名、按钮影响、通过/失败/未执行现象。
- 没有按钮的复杂测试不得交给用户；先补按钮再交付。
- 每个开发阶段都要先建立“测试项目→一键入口”映射；能安全自动化的项目必须一键完成，能安全串行的只读项目必须提供阶段总测试按钮。
- 每个按钮日志必须逐步说明自动做了什么、为什么能验证、实际值、预期值和最终结论；用户只提供日志，表格与判定由 Agent 完成。
- 本阶段日志判定和正式记录完成后，立即删除本阶段测试专用按钮/辅助类，重新构建、同步、核对哈希；不得把已完成按钮留到后续阶段。

## 阶段 4 永久规则

- 精确兼容只影响低权重 Def 回退和 Race 边界，不得压过当前世界实际 Endogene 样本。
- `GD_RaceGeneCompatibilityDef` 的第三方名称必须来自对应模组实际加载的 1.6 Def；不得复制第三方 GeneDef。
- 精确 Race 有规则时，HAR raceGenes 仍可读取，但通用 Xenotype 列表不得重新把基础种族与混血池合并。
- Wolfein 不把不可遗传纯血身份自动转成系谱来源；OA 统一使用 `OAGene_*` Def 名，仅明确可遗传的七个普通鼠族异种类型作为回退，不登记 `OAGene_BiochemicalRatkinI`。
- Ratkin Gene Expanded 与 OA Ratkin 的元数据声明不兼容，游戏内验收必须分开加载。
- 阶段 4 日志已判定通过，`GD_Phase4AutomatedTests.cs` 已删除；不得恢复已完成使命的阶段 4 测试入口。清理后重建、同步、哈希核对与按钮字符串扫描均已完成。



## 阶段 6 当前收尾状态

- 已新增可持久化设置：启用开关、玩家/NPC 阵营开关、保守/标准/高强度、突变比例、新生儿突变概率、普通/特殊跨种族、详细日志、同种族限制、不可遗传异种类型基因开关、Archite 开关。
- 已新增设置窗口、按当前选中 Pawn 设置种族/阵营强度覆盖、统计页面、缓存刷新入口。
- 已新增旧档手动补充工具：选中人物、玩家阵营、当前地图、全世界强警告；跳过婴儿、任务专用人物与无基因追踪器人物；按 `thingIDNumber` 的 WorldComponent 记录已处理人物。
- `GD_Phase6AutomatedTests.cs` 与阶段6临时总测试 Debug Action 已在用户日志判定通过后删除，不得恢复。
- 阶段6用户日志验收通过；最终清理后 strict validate、Release build、sync 与 `sync-verify` 全部通过，最终 DLL SHA-256 为 `7E60C929155D4C76C3D6026C5E4F19AB12AEC5DEDCD2EC8D5A24B0FBE69299E5`。

## 阶段 4.5 与阶段 5 最终状态

- 十一来源规则位于 `1.6/Defs/Compatibility/GD_Phase45HarGeneSources.xml`，对应翻译位于 `GD_Phase45Compatibility.xml`；只登记第三方实际加载的 1.6 Def，不复制第三方 Def。Miho 规则以基础包 `miho.fortifiedoutremer` 为唯一来源条件，不要求 `[SRC]Miho` 同时加载。
- 阶段 4.5 与阶段 5 的临时测试源码均已删除；`GD_Phase45AutomatedTests.cs` 与 `GD_Phase5AutomatedTests.cs` 不得恢复。
- 阶段 5 两轮日志证据均已保存：原五项联动测试通过 5/失败 0/未执行 0；随机基因回归生成 20、清理 20、随机位点 24、成功新增 24、异常 0。
- 清理后严格校验、Release 构建、无 prune 白名单同步、`sync-verify`、源码/DLL 临时按钮字符串扫描全部通过；最终 DLL SHA-256 为 `83FA51F6F9B9575F863B1D423236EA980533D857CC90CEA7BD66288FCFDA82A3`。
- 阶段 6 尚未开始；不得把阶段 6 规划写成已实现功能。
