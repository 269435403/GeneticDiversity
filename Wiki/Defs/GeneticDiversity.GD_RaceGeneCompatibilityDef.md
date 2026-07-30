# GeneticDiversity.GD_RaceGeneCompatibilityDef（共 22 条）

> 中文名/描述来自 DefInjected；缺失时描述回退英文原文。游戏解释为人工维护，重建时按 DefType + defName 自动保留。

| 中文名 | 英文名 | defName | 描述 | 游戏解释 | 来源 |
|---|---|---|---|---|---|
| Dragonian 精确基因兼容 | Dragonian precise gene compatibility | GD_Compat_Dragonian | 登记 Dragonian 的原生异种类型，并把 Dragonborn 基因限制在 Dragonian 种族内传播。 | 世界样本不足时，只从 Dragonian 的两个可遗传异种类型补足原生候选；Dragonborn 身份基因不会扩散到其他种族。 | 1.6 |
| 埃波娜精确基因兼容 | Epona precise gene compatibility | GD_Compat_Epona | 基础埃波娜只使用 Xeno_Epona，不借用独角兽、重战马、米莉拉或混血回退池。 | 为基础埃波娜建立独立回退池；出生与世界基因池计算不会误借独角兽、重战马或混血分支。 | 1.6 |
| 埃波娜重战马精确基因兼容 | Epona Destrier precise gene compatibility | GD_Compat_EponaDestrier | 重战马只使用 Xeno_Destrier，不借用其他埃波娜或混血回退池。 | 为重战马建立独立回退池，只允许其原生异种类型参与该种族的候选补全。 | 1.6 |
| 埃波娜—米莉拉混血精确兼容 | Epona-Milira precise gene compatibility | GD_Compat_EponaMilira | 埃波娜—米莉拉混血只使用 Xeno_EponaMilira，并继续由埃波娜模组负责出生后的异种类型身份修正。 | 为埃波娜—米莉拉混血建立双来源精确池；只有两个来源同时加载时生效，并避开其他埃波娜分支。 | 1.6 |
| 埃波娜独角兽精确基因兼容 | Epona Unicorn precise gene compatibility | GD_Compat_EponaUnicorn | 独角兽只使用 Xeno_Unicorn，不借用其他埃波娜或混血回退池。 | 为独角兽建立独立回退池，只允许其原生异种类型参与该种族的候选补全。 | 1.6 |
| 绮罗族精确基因兼容 | Kiiro precise gene compatibility | GD_Compat_Kiiro | 把 KiiroXenotype 作为绮罗族的精确原生来源，并限制自定义或结构基因的跨种族传播。 | 世界样本不足时以 KiiroXenotype 补足绮罗候选，并阻止自定义基因类或身体外观基因进入其他种族。 | 1.6 |
| Kiiro Maine Coon 精确基因兼容 | Kiiro Maine Coon precise gene compatibility | GD_Compat_KiiroMaineCoon | 登记 Maine Coon 异种类型，并把该来源的全部自定义与结构基因限制在 Kiiro 种族内传播。 | 把缅因分支加入绮罗原生池；该扩展来源的专属基因只能供绮罗使用，可与其他绮罗来源合并。 | 1.6 |
| Kiiro Orange Cat 精确基因兼容 | Kiiro Orange Cat precise gene compatibility | GD_Compat_KiiroOrangeCat | 登记 Orange Cat 异种类型，并把该来源的全部自定义与结构基因限制在 Kiiro 种族内传播。 | 把橘猫分支加入绮罗原生池；该扩展来源的专属基因只能供绮罗使用，可与其他绮罗来源合并。 | 1.6 |
| Kiiro Ragdoll 精确基因兼容 | Kiiro Ragdoll precise gene compatibility | GD_Compat_KiiroRagdoll | 登记 Ragdoll 异种类型，并把该来源的全部自定义与结构基因限制在 Kiiro 种族内传播。 | 把布偶分支加入绮罗原生池；该扩展来源的专属基因只能供绮罗使用，可与其他绮罗来源合并。 | 1.6 |
| Kiiro Siamese 精确基因兼容 | Kiiro Siamese precise gene compatibility | GD_Compat_KiiroSiamese | 登记 Siamese 异种类型，并把该来源的全部自定义与结构基因限制在 Kiiro 种族内传播。 | 把暹罗分支加入绮罗原生池；该扩展来源的专属基因只能供绮罗使用，可与其他绮罗来源合并。 | 1.6 |
| Miho 精确基因兼容 | Miho precise gene compatibility | GD_Compat_Miho | 登记天狐 Miho 的六个可遗传异种类型与全部来源基因，并把耳型、天狐身份等种族专属基因限制在 Miho 内传播。 | 为 Alien_Miho 提供低权重原生异种类型与基因回退；耳型和天狐身份基因仅限 Miho，未加载 Odyssey 时自动忽略 Voidborn 条目。 | 1.6 |
| 米莉拉精确基因兼容 | Milira precise gene compatibility | GD_Compat_Milira | 把 MiliraXenotype 作为米莉拉种族的精确原生来源，并保持与混血种族的基因池分离。 | 在世界缺少米莉拉样本时使用 MiliraXenotype 回退，同时不把埃波娜混血池误算为基础米莉拉池。 | 1.6 |
| MoeLotl 精确基因兼容 | MoeLotl precise gene compatibility | GD_Compat_MoeLotl | 登记 MoeLotl 基础异种类型及 Axolotl 的普通研究基因。 | 在世界样本不足时，从 MoeLotl 基础异种类型和其普通研究基因补足 Axolotl 候选。 | 1.6 |
| NewRatkinPlus 精确基因兼容 | NewRatkinPlus precise gene compatibility | GD_Compat_NewRatkinPlus | 登记鼠族的原生异种类型，并把鼠族身体与外观基因限制在鼠族内传播。 | 为基础鼠族登记原生异种类型与全部来源基因；耳、尾、体型和外观基因只允许鼠族继承。 | 1.6 |
| Nivarian 精确基因兼容 | Nivarian precise gene compatibility | GD_Compat_Nivarian | 登记 Nivarian 的原生异种类型，并把 Nivarian 专属基因限制在 Nivarian 种族内传播。 | 为 Nivarian 的三个原生异种类型建立回退；来源中的非标准专属基因会被限制在 Nivarian 内。 | 1.6 |
| OA 鼠族基因扩展精确兼容 | OA Ratkin Gene Expand precise compatibility | GD_Compat_OARatkinGeneExpand | 把 OA 自定义和身体依赖基因限制在鼠族内；继承自可遗传基类的七个普通鼠族异种类型可作回退，四个生化实验体保持不可遗传。 | 七个可遗传 OA 鼠族分支可补足鼠族候选；四个生化实验体不进入遗传回退，专属身体基因不跨种族。 | 1.6 |
| Oberonia Aurea 精确基因兼容 | Oberonia Aurea precise gene compatibility | GD_Compat_OberoniaAurea | 登记 Oberonia Aurea 鼠族异种类型，并把其适应性基因限制在鼠族内传播。 | 把 Oberonia Aurea 分支并入鼠族原生池；冷热适应等种族依赖基因不会传播给非鼠族。 | 1.6 |
| 鼠族基因扩展精确兼容 | Ratkin Gene Expanded precise compatibility | GD_Compat_RatkinGeneExpanded | 只读取当前加载的 1.6 鼠族异种类型，并把鼠耳、鼠尾和身体外观基因限制在鼠族内。 | 把当前实际加载的八个鼠族分支用于回退，并将来源中的自定义、耳尾和身体外观基因锁定在鼠族。 | 1.6 |
| 独角兽—米莉拉混血精确兼容 | Unicorn-Milira precise gene compatibility | GD_Compat_UnicornMilira | 独角兽—米莉拉混血只使用 Xeno_UnicornMilira，并继续由埃波娜模组负责出生后的异种类型身份修正。 | 为独角兽—米莉拉混血建立双来源精确池；只有两个来源同时加载时生效，并与其他埃波娜分支隔离。 | 1.6 |
| 沃芬精确基因兼容 | Wolfein precise gene compatibility | GD_Compat_Wolfein | 只使用可遗传的沃芬异种类型作为回退，并把沃芬自定义基因类限制在沃芬种族内。 | 仅把可遗传沃芬异种类型用于世界回退；沃芬自定义基因类不会被其他种族随机继承。 | 1.6 |
| Yuran 精确基因兼容 | Yuran precise gene compatibility | GD_Compat_Yuran | 把基础 Yuran 种族限定到其原生异种类型。 | 基础 Yuran 世界样本不足时只使用 YuranXenotype，不会借用 Black Snake Miko 分支。 | 1.6 |
| Yuran Black Snake Miko 精确基因兼容 | Yuran Black Snake Miko precise gene compatibility | GD_Compat_YuranBlackSnake | 把 Black Snake Miko Yuran 种族限定到其原生异种类型。 | Black Snake Miko 世界样本不足时只使用其专属异种类型，与基础 Yuran 回退池保持分离。 | 1.6 |
