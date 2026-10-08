# RPG 云端持续开发

工作流配置本身不等于自动开发已经启用。启用的证明是 GitHub Actions 中的实际运行、Codex 开发步骤成功、独立验证、提交及 Artifact 上传。本仓库不会声称 Codex Cloud 会在会话结束后自行重启：当前会话没有可调用的 Codex Cloud 定时任务接口，持续执行由 GitHub Actions 提供。

**当前用户选择暂不提供 API Key，付费 AI 自动开发保持暂停。** 不需要为本轮人工开发、测试和 ZIP 交付提供 Key。只有未来明确配置仓库变量 `RPG_AUTONOMOUS_ENABLED=true` 才允许开始自动 API 开发；即使仓库原来存在同名 Key，默认也不会调用。小时事件在暂停状态输出 `api_development_paused`，不领取持久化运行时间，不启动 Codex。手动 dry run 可以只读取门卫状态，不生成代码或产生模型调用费用。

## 实际工作流

- `.github/workflows/rpg-continuous-improvement.yml`：**RPG Continuous Improvement**，每小时 UTC 第 17 分钟唤醒，通过 `rpg-automation-state` 分支持久化上次领取时间。距离上次开始至少 18,000 秒才领取下一轮。手动运行支持 `force` 和 `dry_run`。
- `.github/workflows/rpg-project-validation.yml`：推送到 `master`、`main` 或 `rpg/iteration-*` 后验证真实工程，并上传包含完整工程及测试记录的 ZIP；这个工作流不需要 OpenAI Key。
- `.github/prompts/rpg-iteration.md`：每轮开发要求、Skills 使用、真实验证和交接规范。
- `tools/automation/main.py`：可信的持久化门卫、候选补丁导出、哈希核验、独立验证封装及安全发布。

GitHub 定时事件只从仓库的默认分支运行。默认分支必须为包含上述工作流的 `master`，Actions 必须已开启。小时门卫保证两轮开始的最短间隔约五小时；实际通常为五至六小时，平台排队可能更久。仓库长期无活动时 GitHub 可能停用定时任务。这里没有严格定时或永久运行的虚假保证。

## 一次性必要设置

1. 以下设置仅用于未来决定启用付费 AI 开发时。届时在仓库 **Settings → Secrets and variables → Actions → New repository secret** 创建 `OPENAI_API_KEY`。只保存到 GitHub Secret；不要写入源码、提示词、文件或日志。
2. 该 Key 对应的 OpenAI API 项目需要可用额度。GitHub Actions 的 `openai/codex-action` 调用独立 API，不能免费复用 ChatGPT/Codex Cloud 订阅。建议在 API 项目设置预算和告警；实际收费按所选模型的 API 价格计算。
3. 在 **Settings → Actions → General** 开启 Actions，并允许需要的官方 Actions。发布和状态分支需要 `GITHUB_TOKEN` 的 `contents: write` 权限。组织策略或分支保护仍可阻止直接写入；不要关闭保护来掩盖失败。
4. 可选仓库变量 `RPG_CODEX_MODEL` 更换已获授权的 Codex API 模型；默认 `gpt-5.3-codex`。没有模型权限时会保留失败记录，不会声称开发成功。
5. 在 Actions 页运行 **RPG Project Validation and ZIP**，确认编译/回归检查及工程 ZIP 真正成功。准备好 API 额度且决定启用时，创建仓库变量 `RPG_AUTONOMOUS_ENABLED`，值为 `true`。再手动运行 **RPG Continuous Improvement**：先 `dry_run=true` 检查门卫，再用 `dry_run=false, force=true` 验证完整开发链。之后按小时门卫调度。暂停时将此变量改为 `false` 或删除；`force` 不会绕过暂停开关。

没有显式开启变量时，门卫输出 `api_development_paused`，跳过开发；开启变量后没有 `OPENAI_API_KEY`，门卫输出 `missing_api_key`。这些都可以证明工作流能被触发，不能证明无人值守 AI 开发已启用。dry run 不花 API 额度、不领取下一轮时间，也不执行代码生成。

## 每轮执行和保护

工作流使用同一 concurrency group，`cancel-in-progress: false`，防止两轮并发修改。持久状态通过 GitHub API 原子更新，不依赖会消失的 runner 磁盘或普通 Actions cache。模型从领取时的 GitHub master SHA 开始，fetch 远程历史，读取四份轮次记录和待合并迭代分支，避免覆盖用户新提交或无意义重复。

模型使用官方 `openai/codex-action` 固定提交 `bdf19a4a223ec2549a3e2274a0cf61556bc07675`、CLI `0.161.0`、`:workspace` 权限 profile，并在独立非特权 `rpg-codex` 用户下运行。触发者须有仓库写权限或为受信 `github-actions[bot]`；没有 `allow-users: '*'`。模型阶段只有只读 GitHub 权限，不保留 checkout 凭证。模型账号不加入 runner、sudo 或 Docker 组，路径访问通过有限 ACL 授予。门卫只接收 Key 是否存在的布尔值；真实 API Key 只由官方 Action 的受保护代理使用。

模型可以改工程及开发记录。Git 元数据、工作流、可信验证/发布脚本、Skills 和 AGENTS.md 不可写。候选通过补丁及 SHA-256 在全新 runner 上传/下载；验证器和测试取自原始 baseline，实际测试修改后的 C# 源码，不运行候选提供的验证入口。编译后的候选测试程序集在另一独立非特权 `rpg-verify` 用户下执行，清空继承环境、限制运行时间，不能改可信工具或封装验证记录。生成步骤失败会强制使发布证据不合格。

独立发布 runner 只运行原始 baseline 的发布和打包工具。只有验证通过、风险可接受且 master 仍等于本轮 baseline 时，才尝试正常推送到 master。高风险、验证失败、保护规则拒绝或用户已有新提交时，保留到 `rpg/iteration-...` 分支；不 force push，不重置用户历史，不将需要实际运行验证的改动冒称为已验收。候选分支应明确区分可检查的工程 ZIP 与已经通过验证的正式交付。

发布后在持久状态写入结果分支、实际 Commit SHA 和验证状态，下一轮获得这些指针并读取未合并成果。master 中的轮次记录保存 baseline 和可回溯的本轮说明，实际结果 SHA 以状态分支和 Actions Summary 为准。使用 `GITHUB_TOKEN` 的推送通常不会再次触发另一条 push 工作流，因此持续开发工作流自身完成独立验证和 ZIP 上传，不依赖递归触发。

## Skills 在新 runner 中的持续可用性

八个经过来源及许可核对的包已保存在 `.agents/skills/`，跟随 Git checkout，无需访问前一轮云主机。各包包含来源、许可证及必要参考资料，汇总见 `.agents/skills/README.md` 和 `docs/skills-source-manifest.json`。每轮根据 AGENTS.md 枚举、完整阅读、实际应用所有适用 Skills，并在 `SKILLS_USAGE.md` 记录对应成果及无法使用的工具。

Unity MCP 和 imagegen 的指导文档随仓库保存，Actions 并不会因此获得 Unity 编辑器、授权、MCP 服务或 ChatGPT 生图功能。没有这些工具的轮次必须明确记录限制。不要自动安装来历不明的脚本、整套外部游戏项目或付费美术服务。

## 验证、下载与记录

离线验证包括可用 C# 编译/回归和工程资源引用检查；独立工程工作流还运行 `tests/automation` 和 `tests/test_distribution.py`，检查持久化门卫、补丁、LFS、推送边界及打包排除规则。报告必须区分实际通过与未运行。当前标准 runner 不包含授权的 Unity 2022.3.53f1 Editor，未执行原生导入、EditMode/PlayMode 或视觉试玩。相关高风险变更保持分支隔离，直到配置真实的 Unity 验证。

ZIP 使用可信打包器，包含 `Assets`、`Packages`、`ProjectSettings`、必要文档及 `.meta`；排除 `.git`、`Library`、`Temp`、`Logs`、构建缓存和凭证。上传为 Actions Artifact，保留 30 天。每轮 Summary 提供真实 Artifact 下载链接和 Commit 链接。GitHub Artifact 下载需要有仓库访问权限并登录 GitHub，不是永久公开下载地址。session-handoff 工具的本地 `.claude/handoffs/` 不会发布；完成、脱敏并验证的轮次交接记录复制到 `docs/iterations/`，随仓库延续。

仓库 Actions 入口：<https://github.com/jinze909/2D-RPG-Project/actions>。两工作流的 **Run workflow** 可用于复查、重新验证和人工恢复。四个根文档记录实际工作：`DEVELOPMENT_PROGRESS.md`、`KNOWN_ISSUES.md`、`NEXT_ITERATION.md`、`SKILLS_USAGE.md`。失败时保留候选/验证 Artifact，下一轮仍从 GitHub 最新 master 开始，读取未合并迭代记录，继续可安全完成的工作。

查看最近一次门卫 Summary 中的 `Next eligible start (UTC)`，可以得到持久状态计算的下一次可执行时间。小时 cron 的下一次唤醒和平台排队决定实际开始时间；没有 Key 或没有一次真实成功开发记录时，不应提供虚构的“下一轮已确定执行时间”。

## 依据

- 官方 Codex Action：<https://github.com/openai/codex-action>，包括其 unprivileged-user 示例及 permission-profile 文档。
- GitHub scheduled events：<https://docs.github.com/actions/reference/workflows-and-actions/events-that-trigger-workflows#schedule>。
- GitHub workflow token permissions：<https://docs.github.com/actions/security-for-github-actions/security-guides/automatic-token-authentication>。
- GitHub Artifact 下载：<https://docs.github.com/actions/managing-workflow-runs-and-deployments/managing-workflow-runs/downloading-workflow-artifacts>。
