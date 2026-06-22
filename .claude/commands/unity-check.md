---
description: Unity MCP の疎通・コンパイル状態・コンソールエラーを確認する
---

MCP for Unity の接続状態と Unity プロジェクトの健全性を確認してください。

1. `mcpforunity://editor/state` を読み、`ready_for_tools` / `is_compiling` /
   `is_domain_reload_pending` / `blocking_reasons` を報告する。
2. `read_console(types=["error", "warning"], count=20, include_stacktrace=true)` で
   現在のエラー/警告を取得して要約する。
3. ツール群が出ていない場合は `.claude/docs/mcp-unity-workflow.md` の復旧手順を案内する。
4. 問題がなければ「MCP 接続 OK・コンパイル成功・エラーなし」を明示する。

確認のみで、ユーザーの依頼がない限りシーンやアセットは変更しないこと。
