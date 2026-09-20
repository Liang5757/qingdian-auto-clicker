# 发布流程

1. 确认 main 的 CI 通过，完成适用的人工验收，并记录未覆盖范围。
2. 更新 `Directory.Build.props` 中的 Version、CHANGELOG 和必要文档。
3. 运行 `scripts/package.ps1 -Version <版本>`，检查 ZIP 文件和 SHA-256。
4. 将变更通过评审合入 main，创建匹配版本的标签 `v<版本>` 并推送。
5. `release.yml` 在 Windows 重建并测试，再创建 GitHub Release，上传 ZIP 与 SHA-256 文件。
6. 从 Release 下载包，校验哈希并检查解压启动。发布流程不会签名 EXE。

标签版本必须与工程版本一致。不要覆盖已发布标签；修正问题后递增补丁版本。维护者可通过 workflow_dispatch 为已有标签重跑（若发布已存在，流程会失败，需先检查原因，避免静默覆盖）。
