# 2D RPG Project

Unity **2022.3.53f1 国际版**的像素 RPG。当前 `master` 中的
`Assets/Scenes/CombatClearing.unity` 实现战斗、三个敌人、任务封锁与解锁、紧凑 HUD、
胜败重试与持久成长。原玩家原型 `Assets/Scenes/SampleScene.unity` 仍然保留。
工程尚未通过原生试玩，也尚未实现完整世界与剧情。
项目按实际完成的闭环持续扩展，保留原有金发主角。

在 Unity Hub 安装指定版本，克隆后运行 `git lfs pull` 取得真实人物图片，再用
Unity 打开工程及 CombatClearing。WASD/方向键、模拟左摇杆或 D-Pad 移动；
J/空格攻击，K 法力爆发，E 激活北侧信标，胜败后 R 重试，Esc 暂停，M 静音。
手柄已改为直接向量绑定，但尚未执行真实设备测试。P 仅保留在 SampleScene 的
伤害调试中；新关卡关闭该输入。玩法合同与原生验收见
[Combat Clearing](docs/game/COMBAT_CLEARING.md)。

首轮已修复固定物理更新、死亡/停用后的移动状态、输入资源释放、非法伤害和蓝耗。
第二轮补齐独立运行时属性、重试生命周期与复活触发，并替换了原先循环转向的
临时死亡表现；未新增或替换主角图片。
第三轮候选统一了石墙、苔地、守卫和符文信标的 15 色像素美术，增加蓄力与攻击
生效标记、复用的命中/击杀反馈及音效；临时提示改用边缘位置，暂停或死亡后
不再显示不可执行的交互。PR #2、#3、#4 已普通合并到候选，PR #1 已合入 master。
用户最新要求将原生验收设为可选；它仍未执行，不代表原生试玩通过。
第四轮修复了停用后旧攻击/反馈恢复，以及同一时刻多目标命中受敌人顺序影响的问题；
暂停仍冻结现有动作，冷却、资源与奖励保留。原生物理和试玩仍待验收。
第五轮候选加入本地成长存档：完成信标后保存 30 金币，结算时按 1/2（含数字键盘）
购买生命/法力升级，R 开始下一次挑战时生效；每项最多三级，价格 30/45/60。
保存失败可按 E 重试；未入账就按 R 会丢失本次奖励，已有存档不被自动清空。
详见 [成长与存档合同](docs/game/CLEARING_PROGRESSION.md)。
第六轮为三个守卫接入不同战术：近身方框、锁定方向的长枪、保持距离并标记
当前位置的符文。侧移躲枪、离开标记，趁攻击恢复靠近反击；轮廓与边缘提示区分敌人。
沿用战斗输入、蓝耗、冷却及保存成长，不需要新存档。详见
[敌人战术合同](docs/game/SENTINEL_TACTICS.md)。原生碰撞、美术可读性和难度仍待试玩。
注册源已改为 `packages.unity.com`，未升级任何锁定包版本。云端缺少有效 Unity
许可证，尚未完成原生导入、真实碰撞、Animator 视觉或游戏试玩；离线检查不代替它们。

```bash
python3 tools/validate_project.py --root . --output /tmp/rpg-report.json
python3 -m unittest discover -s tests -p 'test_distribution.py'
python3 tools/package_unity_project.py --root . --output /tmp/2D-RPG-Project.zip
```

离线 C# 检查需要 Mono/mcs/csc，可使用系统 `mono-devel` 或本地 Unity 自带工具。
PNG 验证需要 Pillow。脚本从实际工程源码编译，所有程序集写入临时目录。

普通 CI 使用 [RPG Project Validation and ZIP](.github/workflows/rpg-project-validation.yml)，
在代码推送或手动触发后检查工程源码、资源和打包边界，并上传完整工程 ZIP 与报告。
以实际 Actions 运行结果和 Artifact 为交付证据；标准 runner 尚不包含授权 Unity Editor。

第六轮战术 PR #5 已普通合并；本轮测试与交付见
[交付记录](docs/iterations/2026-10-09-iteration-006/publication.md)和
[接力文档](docs/iterations/2026-10-09-iteration-006/handoff.md)。最终源码、CI 与
完整工程 ZIP 记录于独立
[第六轮交付回执](https://raw.githubusercontent.com/jinze909/2D-RPG-Project/rpg-deliveries/iteration-006-delivery.json)。

第五轮实际测试与合并记录见
[交付记录](docs/iterations/2026-10-08-iteration-005/publication.md)和
[接力文档](docs/iterations/2026-10-08-iteration-005/handoff.md)。最终源码、
完整 Unity 工程 ZIP、SHA-256 与对应 CI 位于独立
[交付回执](https://raw.githubusercontent.com/jinze909/2D-RPG-Project/rpg-deliveries/iteration-005-delivery.json)。
第四轮及更早的源码、报告与 ZIP 保留。
ZIP 包含 Assets、Packages、ProjectSettings、全部 .meta、文档及八个 Skills；
这是源码工程，尚无原生玩家构建。

8 个许可清晰的 Skills 随源码保存在 [.agents/skills](.agents/skills/README.md)。
每轮先读 [AGENTS.md](AGENTS.md) 和四份根开发记录。实际工程边界与设计约束见
[设计基线](docs/game/DESIGN_BASELINE.md)。
