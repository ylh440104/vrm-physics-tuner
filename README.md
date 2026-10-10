# N.E.K.O VRM Physics Tuner

N.E.K.O 的配套 Unity 编辑器工具，用来给 VRM 模型调 SpringBone 物理参数。

网页端（`N.E.K.O` 本体）跑 VRM 的时候有个老毛病：碰撞体半径偏大，头发会莫名其妙横着支棱着，像是被一堵看不见的墙挡住。查下来是 UniVRM 导出时碰撞体没跟着缩放走（[vrm-c/UniVRM#673](https://github.com/vrm-c/UniVRM/issues/673)），但网页端拿不到模型原始数据，只能在加载时统一压 50%——**所有模型一刀切，不可能对**。

这个工具就是把那个经验值换掉：在 Unity 里打开模型，按骨骼实际几何算一遍碰撞体，导出一份配置，网页端读它。

它只产出配置，不参与运行时。装到 Unity 里就行，不会进游戏包。

## 装

Unity 2021.3 以上。Package Manager → Add package from git URL：

```
https://github.com/ylh440104/vrm-physics-tuner.git
```

想锁版本就加 `#v0.2.0`。

## 用

菜单 `N.E.K.O` → `VRM Physics Tuner`。

1. 把模型的根节点拖到 **Avatar Root**
2. 点 **Scan**，它会扫出头发/裙摆/尾巴这些带弹簧骨的链
3. 点 **Build Config**，按骨骼实际位置算碰撞体半径
4. 点 **Start Preview**，在 Scene 里看这些链动起来
5. 填 **Export Path**，点 **Export JSON**

## 网页端怎么接

导出的 JSON 放到 `static/vrm/configs/{模型名}_physics.json`，和现有的 `{模型名}_emotion.json` 同目录。

N.E.K.O 的 `vrm_router.py` 会通过 `GET /api/model/vrm/physics_profile/{model_name}` 把它吐出来，`vrm-physics-profile.js` 在模型加载后读它，覆盖 three-vrm 的碰撞体半径和关节参数。

没有配置文件的模型走原来的 50% 回退逻辑，行为不变。

## 预览

点 Start Preview 之后，Scene 视图里会用青色线画出每条链，黄色线框球是碰撞体半径。面板上会显示关节数和最大偏移量。

预览跑的是一个简化的弹簧骨模拟：方向向量每帧受重力、刚度、惯性影响，然后约束到固定骨长。参数改了重新点一次就能看到区别——把 stiffness 拉到 0、gravity_power 拉到 1，链会整个垂下去；反过来 stiffness 到 1 就基本不动。

这个模拟跟 three-vrm 的实现不是逐行对应的，它的用途是让你**看出参数的方向性**，不是精确复刻网页端表现。

## AI 辅助调参

面板下方有一块 Assistant。填一个 OpenAI 兼容的 chat completions 端点、模型名、API key，写一句你想要的效果（比如"头发太硬了，让它软一点"），点 **Ask for Suggestions**。

它会把这套配置连同你的意图发给模型，模型回一组建议，每条建议长这样：

```
hair_front  stiffness -> 0.6
    太硬了，头发几乎不动
```

每条前面有勾选框，默认不勾。你勾哪些就 **Apply Accepted** 应用哪些，没勾的不会动。所有数值都会夹到合法范围（stiffness/drag_force/gravity_power 夹到 0–1，radius 夹到 0.001–1），模型给离谱的数也不会写坏配置。

key 只存在窗口内存里，不会落盘。

请求是异步发的，按钮会变成 "Waiting..."，期间编辑器不会卡住。窗口关掉的话在途请求会被取消。

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
| `gravity_power` | 重力强度 0–1 |
| `gravity_dir` | 重力方向 |
| `hit_radius` | 碰撞检测半径 |

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

## 开发

`Tests~/` 里有一套不依赖 Unity 编辑器的验证（Unity 会自动忽略带 `~` 的目录）：

- `tests.csproj` —— 262 项行为测试，跑在 Unity API 的轻量替身上
- `real-unity.csproj` —— 用 NuGet 上的真实 UnityEngine 程序集编译整个包，确认用到的 API 都存在

```bash
cd Tests~
dotnet run --project tests.csproj -c Release
dotnet build real-unity.csproj -c Release
```

CI 每次 push 都会跑这两步，外加包结构检查和注释检查。见 `.github/workflows/verify.yml`。

## 许可

MIT