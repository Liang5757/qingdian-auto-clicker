# 贡献指南

欢迎修复问题、完善中文/英文文档，以及改进可访问性和多显示器支持。

1. 较大的功能先提交 Issue 说明场景、预期行为和范围。
2. Fork 仓库，从 `main` 创建描述清晰的功能分支。
3. 保持变更聚焦；沿用 `.editorconfig` 和现有 C# 风格，不随意引入运行时依赖。
4. 运行 `scripts/build.ps1`，为行为变更添加回归测试。修改 UI 或原生输入时完成 `docs/TESTING.md` 中适用的人工检查。
5. 提交 PR，说明问题、解决方式、验证结果和已知限制；UI 修改请附不含隐私信息的截图。

提交信息建议采用 `feat:`、`fix:`、`docs:`、`test:`、`ci:` 等前缀。请勿提交凭证、本地配置、用户名路径、二进制构建产物或真实业务数据。

更新 NuGet 依赖后执行 `dotnet restore --force-evaluate`，提交相应 `packages.lock.json`，再运行锁定还原与测试。GitHub Actions 引用固定提交，由 Dependabot 提议升级。

贡献以项目 MIT 许可证提供。不要求额外 CLA。维护者会优先考虑行为清晰、测试充分且不扩大权限需求的变更。
