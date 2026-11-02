<h1 align="center">Mo Xia</h1>

<div align="center">

English | [简体中文](./README.md)

"Mo Xia" is a local-first, cross platform, open source diary application.

An ink box for the pages you would rather keep to yourself.

![GitHub Repo size](https://img.shields.io/github/repo-size/echo-ht/SwashbucklerDiary?style=flat-square&color=FF5722)
[![GitHub license](https://img.shields.io/github/license/echo-ht/SwashbucklerDiary?style=flat-square)](LICENSE.txt)
![Release Download](https://img.shields.io/github/downloads/echo-ht/SwashbucklerDiary/total?style=flat-square)
[![Release Version](https://img.shields.io/github/v/release/echo-ht/SwashbucklerDiary?style=flat-square&color=3cb371)](https://github.com/echo-ht/SwashbucklerDiary/releases/latest)
[![NET 10.0](https://img.shields.io/badge/dotnet-10.0-purple.svg?style=flat-square&color=512bd4)](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-10)

</div>

## 🛠️ Main Technology Stack
- [Blazor Hybrid](https://learn.microsoft.com/aspnet/core/blazor/hybrid/)
- [Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-server)
- [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [Gir.Core](https://gircore.github.io/)
- [Masa Blazor](https://docs.masastack.com/blazor/introduction/why-masa-blazor) 
- [MAUI](https://learn.microsoft.com/dotnet/maui/)
- [Serilog](https://serilog.net/)
- [SqlSugar](https://www.donet5.com/Home/Doc) 
- [Vditor](https://b3log.org/vditor/) 

## 🚀 Download Channels
- [GitHub Releases](https://github.com/echo-ht/SwashbucklerDiary/releases)
- App store listings to be added.

## ⬇️ [Download Guide](./download-guide.md)
See details [./download-guide.md](./download-guide.md) 

## ✨ Characteristics
* Minimalist UI design
* Markdown real-time rendering
* Cross platform (Android, Windows, macOS, Web, Linux)
* Multi size responsive interface layout
* All data is stored locally without the need for networking
* Privacy mode, guarding your secret diary
* Supports images, audio, and video
* Supports mathematical formulas, brain maps, charts, flowcharts, Gantt c harts, time series diagrams, and staff charts
* Supports multiple languages
* Supports dark mode
* Label classification display
* Weather, mood, location
* Export multiple format files
* Support text and image sharing
* Support webdav backup
* Synchronous transmission under the same local area network
* AI note assistant with any OpenAI/DeepSeek-compatible provider, local vector search, and RAG answers
* MCP control for local AI clients to read, create, update, search, and delete notes (private library requires explicit access)

## 🧩 Screenshot
<table>
    <tr>
        <td><img src="./res/screenshots/Windows(1).png"/></td>
        <td><img src="./res/screenshots/Windows(2).png"/></td>
    </tr>
    <tr>
        <td><img src="./res/screenshots/Windows(3).png"/></td>
        <td><img src="./res/screenshots/Windows(4).png"/></td>
    </tr>
 </table>

 <table>
    <tr>
        <td><img src="./res/screenshots/Android(1).png"/></td>
        <td><img src="./res/screenshots/Android(2).png"/></td>
        <td><img src="./res/screenshots/Android(3).png"/></td>
        <td><img src="./res/screenshots/Android(4).png"/></td>
    </tr>
 </table>

## 🖥 Supported Platforms
- Windows (Windows 11 and Windows 10 version 1809 or higher)
- Android (Android 8.0 (API 26) or higher)
- ~~iOS (iOS 14 or higher) (Virtual machine can run, physical machine has not been tested)~~
- macOS (macOS 12 or higher)
- Web
- Linux

## 🌏️ Supported Languages
| Code  | Language               | View                                                             |
| ----- | ---------------------- | ---------------------------------------------------------------- |
| ar-EG | Arabic (Egypt)         | [View](./src/MoXia.Rcl/wwwroot/i18n/ar-EG.json)      |
| de-DE | German                 | [View](./src/MoXia.Rcl/wwwroot/i18n/de-DE.json)      |
| en-US | English (United States)| [View](./src/MoXia.Rcl/wwwroot/i18n/en-US.json)      |
| es-ES | Spanish (Spain)        | [View](./src/MoXia.Rcl/wwwroot/i18n/es-ES.json)      |
| fr-FR | French (France)        | [View](./src/MoXia.Rcl/wwwroot/i18n/fr-FR.json)      |
| ja-JP | Japanese               | [View](./src/MoXia.Rcl/wwwroot/i18n/ja-JP.json)      |
| ko-KR | Korean                 | [View](./src/MoXia.Rcl/wwwroot/i18n/ko-KR.json)      |
| pt-BR | Portuguese (Brazil)    | [View](./src/MoXia.Rcl/wwwroot/i18n/pt-BR.json)      |
| ru-RU | Russian                | [View](./src/MoXia.Rcl/wwwroot/i18n/ru-RU.json)      |
| vi-VN | Vietnamese             | [View](./src/MoXia.Rcl/wwwroot/i18n/vi-VN.json)      |
| zh-CN | Chinese (Simplified)   | [View](./src/MoXia.Rcl/wwwroot/i18n/zh-CN.json)      |
| zh-TW | Chinese (Traditional)  | [View](./src/MoXia.Rcl/wwwroot/i18n/zh-TW.json)      |

## ⌨️ Development Environment
[Visual Studio 2026](https://learn.microsoft.com/visualstudio/install/install-visual-studio?view=visualstudio)

- System requirements

    - Windows 10 version 1909 or higher: Home, Professional, Education, and Enterprise

- Workload

    - Web and Cloud
        - ASP.NET and Web Development
    - Desktop and mobile apps
        - Mobile development using .NET / .NET Multi platform App UI development
        - .NET Desktop Development

[Visual Studio Code](https://code.visualstudio.com/docs)

- Basic environment
 
    - [.Net 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
    - [libwebkitgtk-6.0-4(Optional)](https://packages.ubuntu.com/jammy/libwebkitgtk-6.0-4) (For develop Linux)
    - [OpenJDK 17(Optional)](https://learn.microsoft.com/zh-cn/java/openjdk/download#openjdk-17) (For develop Android)
    - [Xcode(Optional)](https://developer.apple.com/xcode) (For develop macOS)

- Extension

    - [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
    - [.NET MAUI](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui)

## 🏗️ [Project structure](./directory-structure.md)
See details [./directory-structure.md](./directory-structure.md)  

## 📄 License and origin
This project is based on [Yu-Core/SwashbucklerDiary](https://github.com/Yu-Core/SwashbucklerDiary) and is licensed under [AGPL-3.0](./LICENSE.txt). Copyright in the upstream code remains with its contributors.

## 🙏 Thanks to the following Open Source Projects
- [.NET MAUI](https://github.com/dotnet/maui)
- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [betterSearch](https://github.com/jayZOU/betterSearch)
- [Blazored LocalStorage](https://github.com/Blazored/LocalStorage)
- [AspNetCore.Components.WebView.Gtk](https://github.com/Yu-Core/AspNetCore.Components.WebView.Gtk)
- [Brotli](https://github.com/google/brotli)
- [ClosedXML](https://github.com/ClosedXML/ClosedXML)
- [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui)
- [dotnet-packaging](https://github.com/quamotion/dotnet-packaging)
- [EmbedIO](https://github.com/unosquare/embedio)
- [Fly Gesture Unlock](https://github.com/feiafei27/fly-gesture-unlock)
- [Gir.Core](https://github.com/gircore/gir.core)
- [Markdown Guide](https://github.com/mattcone/markdown-guide)
- [MASA.Blazor](https://github.com/BlazorComponent/MASA.Blazor)
- [MauiBlazorToolkit](https://github.com/Yu-Core/MauiBlazorToolkit)
- [maui-linux](https://github.com/MauiGtk/maui-linux)
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore#microsoftdatasqlite)
- [panzoom](https://github.com/anvaka/panzoom)
- [Plugin.Maui.Biometric](https://github.com/FreakyAli/Plugin.Maui.Biometric)
- [Raycoon.Serilog.Sinks.SQLite](https://github.com/RAYCOON/Serilog.Sinks.SQLite)
- [Serilog](https://github.com/serilog/serilog)
- [Serilog.Enrichers.Thread](https://github.com/serilog/serilog-enrichers-thread)
- [Serilog.Extensions.Logging](https://github.com/serilog/serilog-extensions-logging)
- [Serilog.Sinks.Debug](https://github.com/serilog/serilog-sinks-debug)
- [snapdom](https://github.com/zumerlab/snapdom)
- [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw)
- [SqlSugar](https://github.com/DotNetNext/SqlSugar)
- [Swiper](https://github.com/nolimits4web/swiper)
- [Taglib-sharp](https://github.com/mono/taglib-sharp)
- [WebDavClient](https://github.com/skazantsev/WebDavClient)
- [wc-waterfall](https://github.com/huodoushigemi/wc-flow-layout)
- [Vditor](https://github.com/Vanessa219/vditor)
