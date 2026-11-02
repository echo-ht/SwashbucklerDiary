# 墨匣 MCP 控制

墨匣内置一个只监听 `127.0.0.1` 的 MCP 控制服务，并提供一个独立的 stdio MCP 进程。MCP 不直接打开 SQLite，所有读写都复用应用内的笔记、向量索引和 RAG 服务。

## 支持范围

- 支持：GTK 桌面版、Windows/macOS MAUI 桌面版、Blazor Server。
- 不支持：WebAssembly 浏览器版；Android/iOS 不启动本机控制服务。
- 使用期间墨匣必须保持运行。

## 开启

1. 启动墨匣，进入 `AI 设置`。
2. 开启 `启用 MCP 控制` 并保存。
3. 如需让 MCP 访问隐私库，再开启 `允许 MCP 访问隐私库`。
4. 启动 MCP 客户端，并把命令指向 GitHub Release 中的 `MoXia-MCP-*` 可执行文件。

不要从 WebAssembly 页面开启；该平台没有本机控制服务。

## 客户端配置

Claude Desktop、Codex 或其他支持 stdio MCP 的客户端可使用类似配置：

```json
{
  "mcpServers": {
    "moxia": {
      "command": "/path/to/MoXia.Mcp"
    }
  }
}
```

Windows 示例：

```json
{
  "mcpServers": {
    "moxia": {
      "command": "C:\\Tools\\MoXia-MCP-1.0.0-win-x64.exe"
    }
  }
}
```

从源码运行：

```bash
dotnet run --project src/MoXia.Mcp/MoXia.Mcp.csproj
```

检查与正在运行的墨匣是否连通：

```bash
MoXia.Mcp --doctor
```

## 工具

所有工具都接受 `library` 参数：

- `normal`：普通笔记库。
- `private`：隐私库；只有应用设置中开启 `允许 MCP 访问隐私库` 后才可用。

| 工具 | 作用 |
| --- | --- |
| `list_notes` | 分页列出/关键词过滤笔记 |
| `get_note` | 按 GUID 读取完整笔记与标签 |
| `create_note` | 创建笔记并尝试更新向量索引 |
| `update_note` | 更新标题、正文或标签并增量索引 |
| `delete_note` | 永久删除笔记、索引和关系，不可撤销 |
| `search_notes` | 基于向量索引做语义检索 |
| `ask_notes` | 基于检索片段回答问题并返回来源 |
| `get_index_status` | 查看当前库的索引状态 |
| `rebuild_index` | 重建当前库的向量索引 |

## 安全模型

- 控制服务只绑定 IPv4 回环地址，不接受局域网连接。
- 每次启动生成随机临时令牌，写入仅当前用户可读的发现文件；MCP 进程通过令牌访问控制服务。
- MCP 默认关闭；普通库和隐私库完全隔离，隐私库默认不对 MCP 开放。
- 删除工具默认开放，调用即永久删除。请在 AI 客户端中限制可调用工具或做好备份。
- 自定义发现文件位置可设置环境变量 `MOXIA_MCP_DISCOVERY`。

发现文件默认位于：

- `~/.moxia/mcp-control.json`
- 墨匣应用数据目录下的 `mcp-control.json`

应用退出时会尽力删除发现文件。若发现文件残留，启动墨匣后会覆盖它。
