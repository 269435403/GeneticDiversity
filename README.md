# Genetic Diversity (基因多样性)

A RimWorld mod that adds genetic variation to pawns through endogenes, simulating natural genetic diversity and mutation across generations.

## Features

- **Genetic Variation**: Newly generated pawns receive 0-3 random endogenes based on world gene pool
- **Birth Mutations**: 10% chance for newborns to gain one mutation gene
- **World Gene Pool**: Tracks actually existing genes in the world, with priority for same faction/race
- **HAR Compatibility**: Full support for Humanoid Alien Races framework
- **Precise Compatibility**: Built-in support for popular race mods (Kiiro, Milira, Wolfein, Ratkin, Epona, etc.)
- **FRD Integration**: Optional integration with Faction Racial Diversity
- **Customizable Settings**:
  - Intensity presets (Conservative/Standard/High) with probability display
  - Mutation ratio and birth mutation chance
  - Cross-race gene settings
  - Gene blacklist system (by gene or category)
  - Per-faction and per-race intensity overrides
  - Old save backfill tool

## Gene Blacklist System

New in latest version! Players can now exclude unwanted genes:
- **By Gene**: Blacklist individual genes by defName
- **By Category**: Blacklist entire gene categories
- Access via ModSettings → Genetic Diversity → "Manage Blacklist"
- Blacklisted genes are filtered in all random generation

## Requirements

- RimWorld 1.6
- Biotech DLC
- Harmony

## Optional Compatibility

- Humanoid Alien Races (HAR)
- Faction Racial Diversity (FRD)
- Faction Cultural Diversity (FCD)
- Various race mods (see compatibility list in mod settings)

## Installation

1. Subscribe on Steam Workshop
2. Enable in mod list (load after HAR, race mods, and FRD/FCD if used)
3. Start a new game or load existing save

## Configuration

Open **Options → Mod Settings → Genetic Diversity** to customize:
- Enable/disable for player/NPC factions
- Adjust global intensity (see probability distribution)
- Set mutation ratios
- Configure cross-race gene behavior
- Manage gene blacklist
- Set per-faction/race overrides

## Safe Removal

This mod does not define custom genes or xenotypes. All added genes come from other mods. To remove:
1. Make a backup save
2. Disable the mod
3. Load the save (added genes will remain but no new ones will be added)

## Technical Details

- Only adds endogenes (inheritable)
- Does not modify xenotypes or delete core genes
- Respects HAR's CanHaveGene restrictions
- Full safety validation (conflicts, prerequisites, metabolism, race compatibility)
- World gene pool refreshes every 60,000 ticks

## Links

- [Steam Workshop](#)
- [GitHub Repository](https://github.com/yourusername/GeneticDiversity)

## License

See LICENSE file for details.

---

## 简体中文

一个 RimWorld 模组，通过内源基因为人物添加遗传变异，模拟世界中的自然基因多样性和代际突变。

### 主要特性

- **基因变异**：新生成的人物根据世界基因池获得 0-3 个随机内源基因
- **出生突变**：新生儿有 10% 概率获得 1 个突变基因
- **世界基因池**：追踪世界中实际存在的基因，优先使用同阵营/种族基因
- **HAR 兼容**：完全支持人形外星种族框架
- **精确兼容**：内置支持热门种族模组（绮罗、米利拉、狼人、鼠族、马娘等）
- **FRD 联动**：可选整合阵营种族多样性
- **可自定义设置**：
  - 强度预设（保守/标准/高），带概率显示
  - 突变比例和新生儿突变概率
  - 跨种族基因设置
  - 基因黑名单系统（按基因或类别）
  - 按阵营和种族的强度覆盖
  - 旧存档回填工具

### 基因黑名单系统

最新版本新增！玩家现在可以排除不想要的基因：
- **按基因**：通过 defName 禁用单个基因
- **按类别**：禁用整个基因类别
- 通过"模组设置 → 基因多样性 → 管理黑名单"访问
- 黑名单基因在所有随机生成中都会被过滤

### 需求

- RimWorld 1.6
- Biotech DLC
- Harmony

### 可选兼容

- 人形外星种族 (HAR)
- 阵营种族多样性 (FRD)
- 阵营文化多样性 (FCD)
- 各种种族模组（详见模组设置中的兼容列表）

### 安装

1. 在 Steam 创意工坊订阅
2. 在模组列表中启用（在 HAR、种族模组、FRD/FCD 之后加载）
3. 开始新游戏或加载现有存档

### 配置

打开**选项 → 模组设置 → 基因多样性**进行自定义：
- 启用/禁用玩家/NPC 阵营
- 调整全局强度（查看概率分布）
- 设置突变比例
- 配置跨种族基因行为
- 管理基因黑名单
- 设置按阵营/种族覆盖

### 安全移除

本模组不定义自定义基因或异种类型。所有添加的基因都来自其他模组。移除方法：
1. 备份存档
2. 禁用模组
3. 加载存档（已添加的基因会保留，但不会再添加新基因）
