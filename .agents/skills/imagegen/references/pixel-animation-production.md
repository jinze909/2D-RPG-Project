# 像素 RPG 人物动画：一致性、对齐与循环

仓库改写说明（2026-10-08）：开发代理补充可移植环境检查，并将过期的设计缺失、LFS 和主机状态改为每轮核对条件；保留原有研究来源与限制。

本地工作流补充，2026-10-07 查证。用户已明确采用像素风，目标为国际版 Unity 2022.3.53f1。本文件是生产方法与验收规则，不是新模型，也不是已经通过实测的动画成品。

## 选择的路线

固定角色母版和动作模板 → 带参考约束的整段动作生成 → 可编程像素编辑 → 对齐与循环验收 → Unity 导入。

当前对话的内置 imagegen 可生成参考图和动作候选，接口没有逐帧骨架、逐帧冻结蒙版或 closed_loop 参数。提示词中的这些要求只是目标，不能描述成工具的硬控制。默认仍使用内置工具；外部服务的账户、凭据、计费调用和连接并未完成，不自动改用外部 API。

生成和收尾必须分开验收：编辑器能精确控制画布、格子、锚点、色板及时间；角色身份和自然运动需要检查并修复，不接受单次生成即可保证的说法。

## 查到的实际做法

### 专门的像素动画：PixelLab

- 官方 MCP 公开 `create_character` 和 `animate_character(character_id=...)`，可以复用同一个角色 ID 生成动作及方向。这是专用外部服务，不是已经下载就能工作的本地 skill。
- 官方 Python SDK 的 `animate_with_skeleton` 接收 `reference_image`、整段 `skeleton_keypoints`、统一 `image_size`、`color_image`、逐帧 `inpainting_images` 与 `mask_images`。代码将 `color_image` 描述为 “Forced color palette”。这是 SDK 路径，不能假定 MCP 的动作工具也暴露所有这些参数。
- 官方 `animate_with_text` 测试演示用蒙版冻结第一帧。测试只验证图像尺寸和数量，没有验证角色身份或循环质量。
- 查阅的 SDK 未提供专门的 loop/align 参数。固定色板、骨架及首帧约束有助于控制结果；生成后的颜色、位置和循环仍需检查。

### 可自部署的组合

IP-Adapter 提供图像参考，ControlNet 提供姿势/边缘等结构约束，AnimateDiff 提供帧间时间关系。AnimateDiff-Evolved 有闭环上下文选项。这些不是内置 imagegen 的可选参数，也不是只固定 seed 就能获得的能力。

Sprite Sheet Diffusion 的研究实现明确组合 ReferenceNet、Pose Guider 和 Motion Module；推理代码输入同一角色参考和整段姿势。它印证了外观、动作、时间关系分别控制的路线，但有旧依赖、CUDA要求、多份权重及文档缺口，暂不当作成熟生产 skill 安装。

AnimateDiff 官方写明 “Small flickering is noticeable”。闭环上下文能改善首尾关联，不等于像素身份或自然循环的质量保证。当前没有为此下载权重、搭建 GPU 工作流或做实际生图测试。

## 开工前冻结一个资产合同

先为一个方向、一个动作做短样例，验收后再扩展方向和动作。原设计接力已提供；每轮仍须核对当前仓库实际保存点和已确认的主角母版，不能把本流程的通用默认值当作角色造型或剧情决定。

记录并冻结：

- 每个方向的角色母版、头身比例、轮廓、发型、服装、配色、装备、武器所在手。
- 原生像素尺寸、全帧相同的透明画布、安全边距和调色板。
- 地面/root 锚点在画布中的坐标；锚点表达角色世界位置，不等于每帧最低的不透明像素。
- 动作、方向、独立姿态帧数、每帧时长、循环策略、导出顺序。
- 待导入的统一 PPU、pivot 和 Unity 移动速度。

四方向必须保持角色设计一致，但不要强行镜像有单边装备的角色。固定 seed 仅有助复现，不能锁定角色身份。一次生成整页也不能证明准确网格或连续动作。

## 角色一致性：限制需要重新生成的内容

1. 每段动作都引用已经确定的角色母版；不要让每一帧重新设计发型、衣服或装备。
2. 先确定姿势序列，再生成完整动作段。运动模板使用同一画布、视角和身体比例，保持原地运动。
3. 错误帧针对漂移区域修复，保留正确的部分。使用实际工具支持的参考/编辑机制，不能虚构冻结蒙版参数。
4. 在像素编辑器里，姿势不改变的部件可以复用同一 cel 或母版像素，只做需要的整数平移。真正转动、遮挡或变形的部件需重绘；不能把整个走路角色锁成静态图。
5. 最终检查发型、服饰纹样、肤色、武器数量、左右手、头身比例和色板。颜色总数、包围盒或图像相似度只能提示异常，不能判断角色语义一致。

精确色板和像素复用属于编辑器操作。当前使用内置图像工具时，不擅自用 Python 对图像做缩放、量化、平移或补画。用户选择确定性编辑工具后，使用相应编辑器及其 API/CLI；读取像素并输出检查结果本身不改变图像。

## 位置对齐：共同画布和语义锚点

- 保留相同尺寸透明画布和同一 root/地面参考位置，不按每帧包围盒分别居中，不按姿势逐帧缩放。
- 用根节点、骨盆投影或明确绘制的地面标记建立角色坐标。抬脚、长武器、披风及跳跃会改变最低像素和外框，不能用它们自动推断锚点。
- 必要的位置修正只使用整数像素平移。身体合理的上下起伏、手脚摆动应保留；不要把每只脚每一帧都硬拉到地面。
- 统一 pivot 只统一图像坐标原点，不能修复图像内容在画布内画歪的问题。
- 首轮直接导出完整画布的规则网格。Aseprite `--trim` 会逐帧裁切；如果采用它，必须使用 JSON 中的 `spriteSourceSize`/`sourceSize` 重建偏移。Unity 普通网格切片不会自动读取这些字段。
- Unity 的像素画采用 Point、关闭有损压缩、统一 PPU，并按地面锚点设置 Custom pivot。若锚点不在画布底边，BottomCenter 不一定适用。注意编辑器顶左像素坐标与 Unity 底左坐标的换算。

工程输出可以要求：画布尺寸、网格边界、导出順序和 pivot 与合同精确一致；锚点校正只能保证所定义的坐标，不自动证明动作正确。

## 循环：先闭合动作相位，再设置播放循环

走路样例可从 8 帧完整左右脚周期开始，具体帧数服从原有风格和播放速度。每半周期包含接触、下沉、经过、抬升，然后进入另一脚接触。

- 将一个周期分成 N 个相位：0、1/N、…、(N−1)/N。相位 1 与 0 等价，不应作为额外显示一拍的重复终帧。
- 周期相邻帧以及最后一帧→第一帧，都要保持脚步相位、身体高度、朝向、装备位置和运动方向连续。并不要求最后一帧与第一帧完全相同。
- 简单把第一帧复制到末尾不能修复错误相位，还可能造成停顿。走路不能机械地正播后倒播；ping-pong 适合某些往返动作，需单独判断。
- 像素画默认不使用交叉淡化或普通视频光流补帧；这些操作容易产生双影、模糊和额外颜色。
- 普通设置 Loop Time 或 GIF 无限播放只改变播放方式，不会创造自然的动作闭合。
- 支撑脚滑动要在角色实际移动时检查。支撑期脚在画布内的位移需与角色世界位移配合，动画步幅与游戏移速需要匹配，不能只验收原地播放。

## 明确的验收门槛

结构检查与视觉检查分别记录，不用“检查通过”混淆两者。

| 检查项 | 验收方式 | 不能由它证明的内容 |
| --- | --- | --- |
| 画布、网格、帧数、透明度、时长、顺序 | 尺寸/元数据检查，逐格检查是否越界及被裁掉 | 角色身份和动作美感 |
| 色板、重用部件、约定锚点、pivot | 像素及编辑器数据检查，核对显式坐标 | 衣服是否合理、支撑脚是否滑动 |
| 外观和轮廓 | 母版对照、洋葱皮、逐帧切换；按装备清单核对 | 循环时序是否自然 |
| 周期内部和末→首过渡 | 正常速度连续播放至少 10 次，慢速/逐帧复核 | 游戏移速下无滑步 |
| 角色移动时的脚部接触 | Unity 或等价移动预览，检查支撑期 | 没有实际运行时不能宣布通过 |

交付候选需带帧数/时长、方向、循环设置、画布、色板、PPU、pivot及验收记录。未检查、存在缺陷和已经通过要分开写。不交付仍有明显变脸、装备变化、额外位移或循环跳变的版本作为最终动画。

## 编辑器与外部能力的选择

- Aseprite：具备时间轴、洋葱皮、逐帧时长、tags、共享 cel、sprite sheet/JSON导出和可编程编辑能力；发行版有商业许可要求。可用于精确修复而不仅是用户手工检查。
- Pixelorama：免费开源，具备洋葱皮、时间轴、参考线、tags与 sprite sheet 导出。它提供编辑环境，不是自动判定 AI 角色质量的模型。
- PixelLab：优先考察其像素动画及骨架约束能力；需要外部服务账户与安全配置凭据。当前没有接入，也没有发起付费生成。
- ComfyUI组合：更可定制，但需要兼容模型、GPU和工作流调试。当前不把安装一份文档描述成已经具备这个能力。

内置生成工具先完成角色母版和动作候选。若质量不足，选择外部动画生成或可编程像素编辑，不能仅靠换一个 skill 名称宣称问题已解决。

### 让 AI 执行收尾，而不是把逐帧工作交回用户

可用 Aseprite 的 `--batch --script` 执行明确的 Lua 操作：按记录的地面/root 锚点进行整数平移，复制已确认部件，检查颜色，设置帧时长，导出完整网格及元数据。脚本需在实际安装的编辑器上验证，不能仅凭 API 名称宣布通过。无需用户逐帧亲手编辑；缺陷判断仍需图像和播放检查。

已核查 `willibrandon/pixel-mcp` 的固定版本，它是 Aseprite 的本地 MCP 桥而不是 skill，需要 Aseprite 1.3+及 Go。其 `link_cel` 实际调用的 `newCel` 在官方 Aseprite 中复制图像，不能当成已验证的共享 cel；`set_palette` 也不等于将 RGB 像素全部映射到色板。此桥暂不安装。若后续采用它，先验证相关操作；也可直接使用针对任务编写并验证的 Lua 脚本。当前云环境没有 Aseprite、Pixelorama 或已连接的该 MCP。

## 每轮须重新核对的条件

这是可移植生产指导，历史主机状态不作为当前检查结果。先核对真实 PNG 是否已从 Git LFS 取回、当前 importer 的 Point/压缩/PPU/切片/pivot、实际集成架构、Unity 2022.3 许可证以及可用图像/编辑器/MCP 工具。保持已有 .meta GUID 和接受的角色母版。结构检查不能证明自然循环；没有真实图片或原生播放时必须记录相应未验收项。后续 Actions 环境不会自动继承本对话内置 image_gen 或任何外部服务账户。

## 本次查证来源

网络搜索引擎和部分官网在当前环境返回代理 403。本次通过可访问的 GitHub 公开搜索、官方文档源码和实际实现核查；没有把访问失败的网页当成已读材料。

1. PixelLab 官方 MCP：<https://github.com/pixellab-code/pixellab-mcp>
2. PixelLab 骨架动画 SDK，固定源码版本：<https://github.com/pixellab-code/pixellab-python/blob/fe5f6a56ab78a43d9a151ef8ab7ed87c0936aad7/pixellab/animate_with_skeleton.py>
3. PixelLab 首帧冻结示例：<https://github.com/pixellab-code/pixellab-python/blob/fe5f6a56ab78a43d9a151ef8ab7ed87c0936aad7/tests/test_animate_with_text.py>
4. Diffusers 的 IP-Adapter/ControlNet 官方指导：<https://github.com/huggingface/diffusers/blob/main/docs/source/en/using-diffusers/ip_adapter.md>
5. AnimateDiff 实现及限制：<https://github.com/guoyww/AnimateDiff>
6. AnimateDiff-Evolved 闭环上下文实现：<https://github.com/Kosinkadink/ComfyUI-AnimateDiff-Evolved/blob/main/animatediff/context.py>
7. Sprite Sheet Diffusion 实现：<https://github.com/chenganhsieh/Sprite-Sheet-Diffusion>；方法项目页：<https://github.com/chenganhsieh/spritesheet-diffusion/blob/main/index.html>
8. Aseprite 导出选项：<https://github.com/aseprite/aseprite/blob/main/src/app/cli/app_options.cpp>；导出 JSON：<https://github.com/aseprite/aseprite/blob/main/src/app/doc_exporter.cpp>
9. Unity 2022.3 Sprite 与 pivot：<https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Runtime/2D/Common/ScriptBindings/Sprites.bindings.cs>
10. Pixelorama：<https://github.com/Orama-Interactive/Pixelorama>
11. AI 像素编辑桥，查阅版本：<https://github.com/willibrandon/pixel-mcp/tree/96e59ec7dfa828c241ad991287364d5b9f3fc79a>；Aseprite `newCel` 的复制实现：<https://github.com/aseprite/aseprite/blob/v1.3.10/src/app/script/sprite_class.cpp>
