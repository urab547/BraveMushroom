# BraveMushroom - 游戏设计文档

> 版本：1.0 | 最后更新：2026-06-25 | 引擎：Unity (URP 2D)

---

## 1. 游戏概述

### 1.1 一句话描述

**BraveMushroom（勇敢的蘑菇）** 是一款 2D 俯视角自动射击游戏，玩家操控角色在关卡中闪避并消灭所有敌人，通过关卡间的商店购买武器和属性升级，最终击败 Boss 通关。

### 1.2 类型定位

| 标签 | 说明 |
|------|------|
| 核心类型 | 2D 俯视角射击 (Twin-Stick Shooter 变体) |
| 子类型 | Roguelike 商店 + 线性关卡推进 |
| 参考作品 | 吸血鬼幸存者 (自动射击)、元气骑士 (关卡 + 商店) |

### 1.3 核心特征

- **自动战斗**：武器自动锁定最近敌人并射击，玩家专注于走位和闪避
- **关卡间成长**：每通过一关进入商店，用金币购买永久属性加成或更换武器
- **渐进难度**：每关敌人数量和种类递增，最终关为 Boss 战
- **存档持久化**：属性加成、金币、关卡进度通过 JSON 存档持久保存

---

## 2. 核心游戏循环

### 2.1 整体流程

```
┌──────────┐
│  主菜单  │──── 新游戏 ───→ [过场动画] ──→ ┐
│          │──── 继续游戏 ──────────────────→ ┤
└──────────┘                                  │
                                              ▼
                                     ┌─────────────┐
                                ┌──→ │  战斗关卡 N  │
                                │    │  (清怪阶段)  │
                                │    └──────┬──────┘
                                │           │ 全部击杀
                                │           ▼
                                │    ┌─────────────┐
                                │    │    商店      │
                                │    │ (购买/升级)  │
                                │    └──────┬──────┘
                                │           │ 下一关
                                │           ▼
                                │      是最终关？
                                │      /        \
                                │    否          是
                                └──┘            │
                                                ▼
                                       ┌─────────────┐
                                       │  Boss 战    │
                                       └──────┬──────┘
                                              │
                                         ┌────┴────┐
                                         ▼         ▼
                                    ┌────────┐ ┌────────┐
                                    │ 通关   │ │ 死亡   │
                                    │ 结局   │ │ 结算   │
                                    └────────┘ └────────┘
```

### 2.2 单关流程

1. 场景加载，从存档读取当前关卡索引
2. `EnemySpawner` 按配置间隔生成敌人，直到达到总数上限
3. 玩家自动射击、手动走位闪避
4. 击杀敌人掉落金币（概率）和经验球
5. 全部击杀后等待 4 秒，进入商店
6. 商店中购买道具（`Time.timeScale = 0`，游戏暂停）
7. 点击"下一关"，关卡索引 +1，重新加载同一场景

### 2.3 死亡流程

1. 血量归零 → 触发死亡动画 → 冻结所有敌人
2. 等待 1 秒 → 显示"你死了"UI → 等待 1 秒
3. 加载 `GameOverScene`（死亡动画 5 秒 → 菜单）
4. 返回主菜单时 **删除存档**（死亡惩罚：全部重来）

---

## 3. 玩家系统

### 3.1 移动

| 参数 | 默认值 | 说明 |
|------|--------|------|
| 基础速度 | 5.0 | Inspector 可调 |
| 最终速度 | 基础 + MoveSpeed 加成 | 商店购买后实时刷新 |
| 输入方式 | WASD + 方向键 | 使用 Unity New Input System |
| 物理驱动 | Rigidbody2D.linearVelocity | FixedUpdate 中设置 |
| 朝向翻转 | SpriteRenderer.flipX | 根据水平移动方向 |

移动输入经过 `normalized` 处理，确保斜向移动不会比直线快。

### 3.2 生命值

| 参数 | 来源 | 说明 |
|------|------|------|
| 最大血量 | `PlayerStats.GetStat(Health)` | 动态计算，购买后立即生效 |
| 默认最大血量 | 10 | 无 PlayerStats 时的兜底值 |
| 当前血量 | 运行时变量 | 开局等于最大血量 |

- **受伤**：`TakeDamage(int)` 扣减，播放 `PlayerHurt` 音效
- **治疗**：`Heal(int)` 恢复，不超过最大值
- **UI**：Slider 血条 + 文字显示 `当前 / 最大`

### 3.3 武器系统

玩家身上挂载 `WeaponController`，包含一个枪支轴心（GunPivot）和枪口位置（FirePoint）。

#### 自动索敌

- 每帧从 `EnemyHealth.allEnemies` 静态列表中找最近的存活敌人
- 仅在攻击范围（默认 6 单位）内锁定目标
- 无目标时不射击

#### 瞄准

- 枪支通过 `Quaternion.Slerp` 平滑旋转朝向目标
- 旋转速度由 `RotationSpeed` 属性决定（默认 20，可升级）
- 枪口角度超过 90 度时翻转 Y 轴，防止枪图颠倒

#### 射击

- 冷却时间到达后自动开火（二次检查目标仍在射程内）
- 支持多弹头散射：按扇形均匀分布，夹角由 `SpreadAngle` 决定
- 每发子弹注入当前计算后的伤害值
- 射击音效：`Shoot`

#### 武器更换

- 商店可购买武器类物品，包含一个完整的 `WeaponController` 预制体
- 购买时将预制体的属性（子弹预制体、基础伤害、射速、弹数、散布、旋转速度、射程、枪图）复制到玩家当前武器
- 武器 ID 持久化到存档，下次加载时自动恢复

### 3.4 属性体系

共 7 种属性类型，通过 `PlayerStats` 统一管理：

| StatType | 中文名 | 计算公式 | 说明 |
|----------|--------|----------|------|
| `Attack` | 攻击力 | `baseDamage + (int)bonus` | 加法模型 |
| `FireRate` | 射速 | `baseInterval / (1 + bonus)` | 除法模型，加成越高间隔越短 |
| `Health` | 最大血量 | `baseHealth + bonus` | 加法模型 |
| `MoveSpeed` | 移动速度 | `baseSpeed + bonus` | 加法模型 |
| `ProjectileCount` | 子弹数量 | `baseCount + (int)bonus` | 取整，影响散射弹数 |
| `SpreadAngle` | 散布角度 | `baseAngle + bonus`（最小 0） | 负值收窄散布 |
| `RotationSpeed` | 枪支转速 | `baseSpeed + bonus`（最小 2） | 影响瞄准响应速度 |

属性值 = Inspector 基础值 + 存档加成值。购买道具后两者分别更新。

---

## 4. 敌人系统

### 4.1 普通敌人

通过 `EnemyAI` 脚本驱动，支持两种类型：

#### 近战敌人 (Melee)

| 行为 | 说明 |
|------|------|
| 移动 | 向玩家直线移动，到达 `stopDistance` 停下 |
| 攻击 | 进入 `attackRange` 后按 `attackCooldown` 间隔直接扣血 |
| 伤害 | 默认 1 点 |

#### 远程敌人 (Ranged)

| 行为 | 说明 |
|------|------|
| 移动 | 同近战 |
| 攻击 | 进入 `attackRange` 后发射 `EnemyBullet` |
| 弹道 | 子弹朝玩家方向直线飞行，3 秒后消失 |

#### 接触伤害

所有标签为 `Enemy` 的物体上额外挂载 `EnemyDamage` 脚本，碰撞即扣血（默认 1 点），独立于 AI 攻击。

#### 敌人默认数值

| 参数 | 默认值 |
|------|--------|
| moveSpeed | 2.0 |
| stopDistance | 0.5 |
| damage | 1 |
| attackRange | 1.0 |
| attackCooldown | 2.0 秒 |
| maxHealth | 20 |
| 金币掉率 | 50% |

#### 动画状态

- `isWalking` (Bool)：移动时为 true
- `Attack` (Trigger)：攻击时触发
- `Die` (Trigger)：死亡时触发

### 4.2 Boss

通过 `BossAI` 脚本驱动，拥有独立的多阶段行为模式。

#### 行为循环

```
散弹射击 × N 次  ──→  切换冲锋模式  ──→  冲锋接近  ──→  近战前摇  ──→  近战攻击
     ▲                                                                    │
     └────────────────────────── 重置计数 ──────────────────────────────────┘
```

#### 散弹射击阶段

| 参数 | 默认值 | 说明 |
|------|--------|------|
| bulletCount | 5 | 一次发射子弹数 |
| spreadAngle | 15 | 子弹间夹角 |
| rangedDamage | 10 | 单发伤害 |
| shotsBeforeCharge | 2 | 射几轮后切换近战 |
| postShootDelay | 1.2 秒 | 射击后僵直时间 |

- 射击后进入僵直状态（不移动、不攻击）
- 散弹以玩家方向为中心均匀扇形展开

#### 冲锋近战阶段

| 参数 | 默认值 | 说明 |
|------|--------|------|
| chargeSpeed | 10.0 | 冲锋移动速度 (正常 5 倍) |
| meleeRange | 2.2 | 近战判定范围 |
| meleeDamage | 20 | 近战伤害 |
| minContactDistance | 1.6 | 最小接触距离，防穿模 |
| meleePreparationTime | 0.5 秒 | 前摇蓄力时间 |
| meleeDamageDelay | 0.5 秒 | 攻击动画到伤害判定的延迟 |

#### 近战前摇视觉

1. Boss 进入冲锋模式，以 `chargeSpeed` 快速接近玩家
2. 到达 `meleeRange` 后停下，开始前摇
3. 脚下红色圆圈指示器渐变显现（透明度 0 → 0.6），持续 `meleePreparationTime`
4. 前摇完成后触发攻击动画，红圈消失
5. 经过 `meleeDamageDelay` 后进行距离检测，仍在范围内则扣血
6. 重置射击计数，回到散弹阶段

#### Boss 血条

- 使用场景中 Canvas 下的 `BossHealthUI` Slider
- 受伤时平滑减少（Lerp 动画）
- 死亡时血条归零并淡出

#### Boss 音效

| 时机 | 音效名 |
|------|--------|
| 散弹射击 | `BossShoot` |
| 近战蓄力 | `BossCharge`（音调自动适配蓄力时长） |
| 近战挥击 | `BossMelee` |

### 4.3 敌人生成规则

`EnemySpawner` 使用 `LevelEnemyConfig` 列表配置每关的敌人数据：

```
LevelEnemyConfig {
    levelName: string        // 关卡名称
    totalEnemies: int        // 本关敌人总数
    spawnInterval: float     // 生成间隔（秒）
    enemyPrefabs: List       // 可生成的敌人预制体列表（随机选取）
}
```

- 生成位置：以玩家为圆心、`spawnRadius`（10 单位）为半径的圆环边缘随机取点
- 防卡墙：使用 `Physics2D.OverlapCircle` 检测障碍物图层，最多重试 15 次
- 生成完毕后不再刷怪，等待全部击杀

### 4.4 敌人死亡

1. 标记 `isDead = true`
2. 触发 `Die` 动画
3. 禁用所有其他 MonoBehaviour 脚本
4. 停止所有协程
5. 禁用碰撞体
6. 停止物理模拟（防尸体滑行）
7. 通知 `GameLevelManager.OnEnemyKilled()`
8. 概率掉落金币
9. 普通敌人：0.5 秒后播放 `Explode` 音效并销毁
10. Boss：2 秒后血条淡出并销毁

---

## 5. 战斗系统

### 5.1 伤害流程

```
玩家武器射击
    │
    ▼
Bullet 生成（携带 actualDamage）
    │
    ▼
OnTriggerEnter2D 碰撞检测
    │
    ├── 碰到 Enemy → EnemyHealth.TakeDamage(damage) → 子弹回收
    ├── 碰到 Wall  → 子弹回收
    └── 超过存活时间 → 子弹回收
```

```
敌人攻击
    │
    ├── 近战敌人 → 直接调用 PlayerHealth.TakeDamage(damage)
    ├── 远程敌人 → 生成 EnemyBullet → 碰到 Player → TakeDamage
    ├── 接触伤害 → OnCollisionEnter2D → TakeDamage
    └── Boss 近战 → 延迟判定 → 范围内则 TakeDamage
```

### 5.2 子弹对比

| 属性 | 玩家子弹 (Bullet) | 敌人子弹 (EnemyBullet) |
|------|-------------------|----------------------|
| 默认速度 | 10 | 5 |
| 默认伤害 | 10（由武器覆盖） | 1（由 AI 设置） |
| 存活时间 | 3 秒 | 3 秒 |
| 碰撞目标 | Enemy, Wall | Player, Wall |
| 方向 | 枪口朝向（本地右方） | SetDirection 指定 |

### 5.3 碰撞规则

| 碰撞类型 | 检测方式 | 触发条件 |
|----------|----------|----------|
| 子弹 → 敌人/玩家 | OnTriggerEnter2D | 子弹使用 Trigger Collider |
| 敌人 → 玩家(接触) | OnCollisionEnter2D | 双方使用普通 Collider |
| 子弹 → 墙壁 | OnTriggerEnter2D | 墙壁标签为 "Wall" |

---

## 6. 经济与进度系统

### 6.1 金币

金币分为两种状态：

| 状态 | 变量 | 说明 |
|------|------|------|
| 临时金币 | `GameLevelManager.tempGold` | 当前关卡内捡到的，尚未结算 |
| 已结算金币 | `SaveData.coins` | 通关进入商店时写入存档 |

- UI 显示的是两者之和（`cachedSavedCoins + tempGold`）
- 只有通关进入商店时，临时金币才会结算到存档
- 如果中途退出游戏，临时金币丢失（但存档中标记了 `isInShop` 状态可恢复商店会话）

金币来源：
- 敌人死亡掉落（50% 概率，每个 1 金）
- 金币行为：掉落后静止 0.5 秒，然后加速飞向玩家，触碰后拾取

### 6.2 经验值与等级

| 参数 | 默认值 | 说明 |
|------|--------|------|
| 初始等级 | 1 | |
| 升级所需经验 | 10 | 每次升级后乘以 1.2 |
| 经验球给予量 | 5 | 每个经验球 |
| 磁吸范围 | 3 单位 | 进入范围后飞向玩家 |

> **注意**：升级系统目前仅有经验条 UI，尚未实现升级奖励选择界面（代码注释标记为"待实现"）。

### 6.3 关卡推进

- 关卡索引从 1 开始，存储在 `SaveData.currentLevelIndex`
- 每次通关商店点击"下一关"后索引 +1
- 所有关卡使用同一个场景文件 `Level_1.unity`，通过索引加载不同的 `LevelEnemyConfig`
- 当索引达到 `finalLevelIndex`（默认 3）时为 Boss 关
- Boss 关通关后加载 `EndingScene`
- 如果索引超过配置数量，循环使用最后一关的配置

---

## 7. 商店系统

### 7.1 商品数据结构

每个商品是一个 `ShopItemData` ScriptableObject：

```
ShopItemData {
    itemName: string          // 显示名称
    icon: Sprite              // 图标
    price: int                // 价格（金币）
    description: string       // 描述文本
    weaponPrefab: GameObject  // [可选] 武器预制体，非空则为武器类商品
    modifiers: List<StatModifier>  // [可选] 属性修改列表
}

StatModifier {
    type: StatType            // 要修改的属性
    value: float              // 修改值（支持负数）
}
```

商品可以同时包含武器和属性修改，也可以只包含其一。

### 7.2 商店流程

1. 通关后游戏暂停（`Time.timeScale = 0`）
2. 从全部商品池中随机抽取（不重复）填充每个商店格子
3. 玩家点击购买：
   - 检查金币是否足够
   - 扣除金币
   - 如有武器：更换武器数据 + 存储武器 ID
   - 如有属性修改：更新 PlayerStats + 刷新相关组件（血条/武器/速度）
   - 一次 Load → 修改 → 一次 Save（合并 I/O）
   - 格子显示"已售"
4. 金币不足时弹出浮动提示"金币不足！"

### 7.3 刷新机制

- 商店提供刷新按钮，花费 `refreshCost`（默认 5 金币）重新随机商品
- 刷新后所有格子重新填充，已售状态重置

---

## 8. 存档系统

### 8.1 技术方案

- 格式：JSON（`JsonUtility`）
- 路径：`Application.persistentDataPath + "/savegame.json"`
- 读写：`SaveSystem` 静态类，`Load()` / `Save()` / `DeleteSave()`

### 8.2 数据结构

```
SaveData {
    // 游戏进度
    currentLevelIndex: int       // 当前关卡（从 1 开始）
    coins: int                   // 已结算金币
    currentWeaponID: string      // 当前武器预制体名称
    isInShop: bool               // 是否在商店中（用于会话恢复）

    // 属性加成（聚合存储，同类型合并为一条）
    statBonuses: List<StatBonus> // [{type, value}, ...]

    // 音量设置（新游戏时保留）
    masterVolume: float          // 默认 1.0
    musicVolume: float           // 默认 0.5
    sfxVolume: float             // 默认 1.0
}
```

### 8.3 存档时机

| 时机 | 操作 |
|------|------|
| 通关进入商店 | 结算金币 + 标记 `isInShop = true` |
| 商店购买物品 | 扣钱 + 写属性 + 写武器 ID（单次写入） |
| 商店刷新 | 扣钱 |
| 点击"下一关" | 关卡 +1 + 标记 `isInShop = false` |
| 调节音量 | 实时保存 |

### 8.4 新游戏重置策略

1. 读取旧存档备份音量设置
2. 删除存档文件
3. 创建新 SaveData（默认值：关卡 1、金币 0、无武器）
4. 恢复音量设置
5. 写入磁盘

### 8.5 会话恢复

如果玩家在商店中退出游戏：
- `isInShop` 为 true
- 下次进入关卡时，直接跳过战斗阶段，打开商店
- 禁用刷怪器

---

## 9. 场景流程与 UI

### 9.1 场景清单

| 场景名 | 用途 | 管理脚本 |
|--------|------|----------|
| `MainMenu` | 主菜单、设置、过场 | `MainMenuController` |
| `Level_1` | 所有关卡（循环加载） | `GameLevelManager`, `EnemySpawner`, `PauseMenu` |
| `GameOverScene` | 死亡结算 | `GameOverController` |
| `EndingScene` | 通关结局 | `EndingController` |

### 9.2 主菜单

```
┌─────────────────────────┐
│       主菜单面板         │
│                         │
│   [ 新游戏 ]            │   ← 有存档时弹出确认覆盖对话框
│   [ 继续游戏 ]          │   ← 无进度时灰显不可点
│   [ 设置 ]              │
│   [ 退出 ]              │
└─────────────────────────┘
```

- **继续游戏判定**：关卡 > 1，或有金币，或有武器 → 可继续
- **新游戏流程**：重置存档 → 播放过场视频（可按 Enter 跳过，需按两次确认）→ 淡出过渡 → 加载关卡
- **设置面板**：三个音量滑条（总音量 / 音乐 / 音效）

### 9.3 游戏 HUD

关卡场景中显示：
- 血条（Slider + 文字）
- 经验条（Slider）
- 金币数量（文字）
- 关卡标题（"第 N 关" 或 "最终决战"，Boss 关为红色）
- Boss 血条（Boss 出现时显示在 Canvas 中）

### 9.4 暂停菜单

- ESC 键唤出 / 关闭
- `Time.timeScale = 0`（游戏完全暂停）
- 包含音量滑条、返回主菜单、退出游戏

### 9.5 死亡场景

1. 播放死亡动画面板 5 秒（可点跳过）
2. 显示菜单：返回主菜单（删除存档）/ 退出游戏

### 9.6 通关场景

1. 播放故事面板 5 秒（可点跳过）
2. 显示胜利面板：返回主菜单（删除存档）/ 退出游戏

> **设计意图**：无论死亡还是通关，返回主菜单都会删除存档，确保每次游戏是完整的一轮体验。

---

## 10. 音频设计

### 10.1 背景音乐

| 场景 | BGM | 切换时机 |
|------|-----|----------|
| 主菜单 | `menuBgm` | 进入主菜单 |
| 普通关卡 | `normalBgm` | 关卡 < finalLevelIndex |
| Boss 关 | `bossBgm` | 关卡 >= finalLevelIndex |

- 切换方式：淡出当前 → 淡入新曲（`fadeDuration` 默认 1 秒）
- 使用 `Time.unscaledDeltaTime` 确保暂停时仍能淡入淡出
- 相同音乐不会重复切换

### 10.2 音效

| 音效名 | 触发时机 | 特殊处理 |
|--------|----------|----------|
| `Shoot` | 玩家射击 | 随机音调 0.8-1.2 |
| `PlayerHurt` | 玩家受伤 | 随机音调 2.0-3.0 |
| `PlayerDeath` | 玩家死亡 | 随机音调 4.0-5.0 |
| `Coin` | 拾取金币 | 随机音调 0.9-1.2（连续拾取有层次感） |
| `Explode` | 普通敌人死亡 | 随机音调 0.8-1.0 |
| `Victory` | 通关 | 正常音调 |
| `BossShoot` | Boss 散弹 | 正常音调 |
| `BossCharge` | Boss 蓄力 | 音调自动匹配蓄力时长 |
| `BossMelee` | Boss 挥击 | 正常音调 |

### 10.3 音量架构

三层音量控制：

| 层级 | 控制方式 | 影响范围 |
|------|----------|----------|
| 总音量 (Master) | `AudioListener.volume` | 全局所有声音 |
| 音乐音量 (Music) | `bgmSource.volume` | 仅背景音乐 |
| 音效音量 (SFX) | `sfxSource.volume` | 仅音效 |

- 可选 AudioMixer 支持（`SettingsMenu` 中配置）
- 所有音量设置实时保存到 JSON 存档
- 通过 `AudioManager.SetAndSaveMasterVolume/Music/SFX` 统一接口操作

---

## 11. 技术架构

### 11.1 脚本分层

```
┌─ 核心管理层 (Singleton) ──────────────────────────────┐
│  GameLevelManager  - 关卡流程、商店、购买、场景切换     │
│  PlayerStats       - 属性中心，合并基础值+存档加成       │
│  AudioManager      - 全局音效/音乐 (DontDestroyOnLoad) │
│  ObjectPool        - 对象池，复用子弹/金币              │
└───────────────────────────────────────────────────────┘

┌─ 玩家层 ──────────────────────────────────────────────┐
│  PlayerController  - 移动输入                          │
│  PlayerHealth      - 血量、受伤、死亡                   │
│  WeaponController  - 自动索敌、射击                     │
│  PlayerWallet      - 金币 UI                           │
│  LevelSystem       - 经验/等级                         │
└───────────────────────────────────────────────────────┘

┌─ 敌人层 ──────────────────────────────────────────────┐
│  EnemyAI           - 普通敌人行为                      │
│  BossAI            - Boss 行为                         │
│  EnemyHealth       - 敌人血量、死亡、掉落               │
│  EnemyDamage       - 接触伤害                          │
│  EnemySpawner      - 按配置生成敌人                     │
└───────────────────────────────────────────────────────┘

┌─ 弹道 / 拾取层 ──────────────────────────────────────┐
│  Bullet            - 玩家子弹                          │
│  EnemyBullet       - 敌人子弹                          │
│  CoinController    - 金币飞行+拾取                      │
│  ExpOrb            - 经验球磁吸+拾取                    │
│  FloatingText      - 浮动提示文字                      │
└───────────────────────────────────────────────────────┘

┌─ 数据层 ──────────────────────────────────────────────┐
│  SaveData          - 存档数据结构                      │
│  SaveSystem        - JSON 读写                         │
│  SaveManager       - 新建/检查存档                     │
│  ShopItemData      - 商品 ScriptableObject             │
└───────────────────────────────────────────────────────┘

┌─ UI / 场景层 ─────────────────────────────────────────┐
│  MainMenuController - 主菜单                           │
│  PauseMenu          - 暂停菜单                         │
│  SettingsMenu       - 设置面板                         │
│  GameOverController - 死亡场景                         │
│  EndingController   - 通关场景                         │
│  ShopSlot           - 商店格子 UI                      │
│  GoldUIDisplay      - 金币 HUD                        │
│  CameraFollow       - 平滑跟随相机                     │
└───────────────────────────────────────────────────────┘
```

### 11.2 单例清单

| 脚本 | 保护方式 | 跨场景 |
|------|----------|--------|
| `AudioManager` | 重复检测 + `DontDestroyOnLoad` | 是 |
| `GameLevelManager` | 重复检测 + Destroy | 否 |
| `PlayerStats` | 重复检测 + Destroy | 否 |
| `ObjectPool` | 重复检测 + Destroy | 否 |

### 11.3 数据流向

```
[JSON 存档文件]
      │
      ▼
  SaveSystem.Load()
      │
      ├──→ PlayerStats (加载属性加成到内存字典)
      ├──→ GameLevelManager (关卡索引、金币缓存、商店状态、武器恢复)
      ├──→ EnemySpawner (关卡配置索引)
      ├──→ AudioManager (音量初始化)
      └──→ MainMenuController (继续按钮状态、音量滑条)

  [玩家操作]
      │
      ├── 购买道具 → PlayerStats.ApplyItem(item, data) → SaveSystem.Save()
      │                    │
      │                    ├──→ PlayerHealth 刷新血条
      │                    ├──→ WeaponController 刷新射击属性
      │                    └──→ PlayerController 刷新移动速度
      │
      ├── 通关结算 → tempGold 写入 SaveData.coins → Save()
      ├── 下一关   → currentLevelIndex++ → Save() → 重载场景
      └── 调音量   → AudioManager.SetAndSave*() → Save()
```

### 11.4 对象池

`ObjectPool` 管理高频创建/销毁的对象，减少 GC 压力：

| 池化对象 | 创建者 | 回收条件 |
|----------|--------|----------|
| `Bullet` | `WeaponController` | 碰撞 / 超时 |
| `EnemyBullet` | `EnemyAI`, `BossAI` | 碰撞 / 超时 |
| `Coin` | `EnemyHealth`（掉落） | 玩家拾取 |

- 静态接口 `ObjectPool.Spawn()` / `ObjectPool.Despawn()` 替代 `Instantiate` / `Destroy`
- 无 ObjectPool 实例时自动降级为原生创建/销毁
- 每个池化对象通过 `PoolTag` 组件记录来源 prefab ID

---

## 12. 已知待实现功能

根据代码中的注释和未使用的接口，以下功能已有框架但尚未完整实现：

| 功能 | 当前状态 | 相关代码 |
|------|----------|----------|
| 升级奖励选择 | 有经验条和等级系统，但升级后无技能选择界面 | `LevelSystem.LevelUp()` 注释"未来添加升级技能选择界面" |
| 经验球掉落 | `ExpOrb` 脚本完整，但无代码主动生成经验球 | `ExpOrb.cs` 存在但无 Instantiate 调用点 |
| 敌人死亡音效 | 代码存在但被注释掉 | `EnemyHealth.Die()` 中 `EnemyDeath` 音效被注释 |
