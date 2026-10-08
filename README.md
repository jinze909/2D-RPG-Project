# 2D RPG Project

Unity **2022.3.53f1 国际版**的像素 RPG。当前 `master` 是可移动玩家原型，入口为
`Assets/Scenes/SampleScene.unity`；完整 Starfall Frontier 的地图、职业、战斗和 HUD
尚未迁入这个仓库。项目将按实际完成的闭环持续扩展，保留原有金发主角。

在 Unity Hub 安装指定版本，克隆后运行 `git lfs pull` 取得真实人物图片，再用
Unity 打开工程及 SampleScene。使用 WASD/方向键移动，P 是原有伤害调试键。
现有摇杆绑定为数字方向复合输入，尚不能承诺硬件摇杆半速移动。

首轮已修复固定物理更新、死亡/停用后的移动状态、输入资源释放、非法伤害和蓝耗。
注册源已改为 `packages.unity.com`，未升级任何锁定包版本。云端缺少有效 Unity
许可证，尚未完成原生导入、真实碰撞、Animator 视觉或游戏试玩；离线检查不代替它们。

```bash
python3 tools/validate_project.py --root . --output /tmp/rpg-report.json
python3 -m unittest discover -s tests/automation -p 'test_*.py'
python3 tools/package_unity_project.py --root . --output /tmp/2D-RPG-Project.zip
```

离线 C# 检查需要 Mono/mcs/csc，可使用系统 `mono-devel` 或本地 Unity 自带工具。
PNG 验证需要 Pillow。脚本从实际工程源码编译，所有程序集写入临时目录。

持续开发配置、独立 API 计费和启用证明要求见 [AUTOMATION](docs/AUTOMATION.md)。
工作流每小时第 17 分钟检查一次持久状态，至少间隔 5 小时才启动新开发；GitHub
排队可能延迟。必须有实际运行记录才可称自动化已启用。

8 个许可清晰的 Skills 随源码保存在 [.agents/skills](.agents/skills/README.md)。
每轮先读 [AGENTS.md](AGENTS.md) 和四份根开发记录。实际工程边界与设计约束见
[设计基线](docs/game/DESIGN_BASELINE.md)。
