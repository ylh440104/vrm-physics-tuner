# VRM Physics Tuner

给 VRM 模型调 SpringBone 物理参数用的 Unity 编辑器工具。

做这个东西的起因很简单：网页端跑 VRM 的时候，碰撞体半径老是偏大，头发会莫名其妙横着支棱着。查下来是 UniVRM 导出时碰撞体没跟着缩放走（[vrm-c/UniVRM#673](https://github.com/vrm-c/UniVRM/issues/673)），但网页端拿不到模型原始数据，只能靠经验值硬压——现在压的是 50%，对所有模型一刀切。

这工具就是来解决这个的：在 Unity 里打开模型，按骨骼实际几何自动算一遍碰撞体，导出一份配置，网页端读它，就不用再猜了。

## 装

Unity 2021.3 以上。Package Manager → Add package from git URL：

```
https://github.com/ylh440104/vrm-physics-tuner.git
```

想锁版本就加 `#v0.1.0`。

## 用

菜单 `N.E.K.O` → `VRM Physics Tuner`。

1. 把模型的根节点拖到 **Avatar Root**
2. 点 **Scan**，它会扫出头发/裙摆/尾巴这些带弹簧骨的链
3. 点 **Build Config**，按骨骼实际位置算碰撞体半径
4. 填 **Export Path**，点 **Export JSON**

导出的 JSON 长这样：

```json
{
  "version": 1,
  "model": "sister1.0",
  "colliders": [
    { "bone": "J_Bip_C_Head", "shape": "capsule", "radius": 0.081, "offset": [0,0,0], "tail": [0,0.15,0] }
  ],
  "chains": [
    {
      "name": "hair_front:hair_front",
      "bones": ["hair_front_01", "hair_front_02"],
      "bone_paths": ["Root/Head/hair_front_01", "Root/Head/hair_front_01/hair_front_02"],
      "stiffness": 0.9,
      "drag_force": 0.4,
      "gravity_power": 0.05,
      "gravity_dir": [0,-1,0],
      "hit_radius": 0.025
    }
  ]
}
```

## 它是怎么算的

**骨骼分类**靠名字。先按 `_`、`-`、大小写边界拆词，去掉 `J`/`Bip`/`L`/`R` 这类噪声词，再拿剩下的词去匹配关键词表——`hair`、`skirt`、`ponytail`、`tail`、`cape`、`ribbon` 等等。

这里有个坑值得说一下：`forearm` 里含 `ear`，`UpperChest` 里含 `chest`。直接做子串匹配会误判，所以必须按完整词匹配。测试里专门锁了这几条。

**碰撞体半径**按几何算，不是猜：

- 骨骼有子节点 → 做成胶囊体，取布料锚点（挂在骨头上的弹簧骨子节点）到骨轴的最大距离
- 没有子节点 → 做成球体，取到骨骼中心的最大距离

如果一根骨头上没挂任何布料锚点，就退回按骨长取一个比例，避免半径塌成 0。

**注意**：这个算出来的半径偏保守（只会偏小，不会偏大）。宁可头发稍微穿一点，也别让它像被一堵看不见的墙挡住。真出问题就把 Export 前的 Padding 调大一点。

## 配置格式

`colliders[]`

| 字段 | 说明 |
|---|---|
| `bone` | 骨骼名 |
| `bone_path` | 层级路径，用来区分同名骨骼 |
| `shape` | `sphere` 或 `capsule` |
| `radius` | 半径，世界单位 |
| `offset` | 相对骨骼的偏移 |
| `tail` | 胶囊体尾端（球体为 `null`） |

`chains[]`

| 字段 | 说明 |
|---|---|
| `name` | 链名 |
| `bones` / `bone_paths` | 骨骼名列表 / 对应路径列表 |
| `stiffness` | 刚度 0–1 |
| `drag_force` | 阻尼 0–1 |
| `gravity_power` | 重力强度 |
| `gravity_dir` | 重力方向 |
| `hit_radius` | 碰撞检测半径 |

## 已知问题

**没在真 Unity 里跑过。** 开发环境是 arm64 Linux，Unity 编辑器只有 x86_64 版本，4.5GB 的包按当时 9.6KB/s 的网速得下五天，装不了。

退而求其次，代码是拿 NuGet 上真实的 UnityEngine 程序集（2021.3.33）编译验证的，0 error 0 warning；另外写了元数据检查逐个核对用到的 API 确实存在，21 项全过。

但编辑器 UI 交互、真 VRM 导入后的实际层级差异、运行时物理表现——这三样必须有人开 Unity 验一次。

## 许可

MIT
