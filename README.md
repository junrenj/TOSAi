# TOS AI 学习平台

这是一个 Windows WPF 桌面端原型，用于跑通教师端、学生端、家长端三套入口。当前重点是平台框架和数据流，不实现 AI 分析逻辑。

## 当前平台流程

```text
学生完成作业/考试
        ↓
学校扫描设备录入数据
        ↓
服务器统一保存学生、作业、考试、报告、学习计划等数据
        ↓
Windows App 根据角色展示不同权限范围的数据
        ↓
教师端：查看全量学生数据，布置针对化作业
学生端：查看本人任务、进度、拍照搜题入口
家长端：查看孩子成绩趋势、报告、心理关注和家庭建议
```

## 三端入口

程序左侧可以切换当前端：

- 教师端：教师总览、学生档案、成绩录入、学生趋势、作业管理、报告中心
- 学生端：学生首页、我的作业、学习进度、拍照搜题
- 家长端：家长首页、成绩趋势、学习报告、心理关注

## 服务端接口抽象

当前真实服务器尚未接入，页面数据来自：

```text
Services/IPlatformApiClient.cs
Services/MockPlatformApiClient.cs
```

后续接服务器时，新增一个 `HttpPlatformApiClient` 实现 `IPlatformApiClient`，再把 `MainWindow.xaml.cs` 里的：

```csharp
private readonly IPlatformApiClient _apiClient = new MockPlatformApiClient();
```

替换成真实 API 客户端即可。页面层不需要大改。
## 运行

```powershell
dotnet run --project TOSAi.TeacherApp\TOSAi.TeacherApp.csproj
```

也可以直接运行编译后的程序：

```powershell
.\TOSAi.TeacherApp\bin\Debug\net9.0-windows\TOSAi.TeacherApp.exe
```

## 当前结构

```text
TOSAi.TeacherApp/
  MainWindow.xaml              # 主窗口、左侧导航、顶部标题、页面承载区
  Styles/Theme.xaml            # 全局颜色、卡片、按钮、导航、表格样式
  Views/                       # 各功能页面
  Models/                      # 学生、成绩、学科分析等数据模型
  Services/                    # 示例数据、CSV 模板服务、AI 分析服务接口
  Templates/成绩导入模板.csv    # 可用 Excel 打开的成绩导入模板
```


## 本地后端 API

当前已新增 `TOSAi.Api`，用于模拟未来云端服务器。默认本地地址：

```text
http://localhost:5088
```

启动后端：

```powershell
dotnet run --project TOSAi.Api\TOSAi.Api.csproj --urls http://localhost:5088
```

可测试接口：

```powershell
Invoke-RestMethod http://localhost:5088/
Invoke-RestMethod http://localhost:5088/api/platform/Teacher/teacherAssignments
```

WPF 端默认请求该本地 API。也可以通过环境变量覆盖服务端地址：

```powershell
$env:TOSAI_API_BASE_URL="https://tosai.onrender.com"
dotnet run --project TOSAi.TeacherApp\TOSAi.TeacherApp.csproj
```

平台页面接口不可用时会自动回退到 `MockPlatformApiClient`，保证界面仍可运行。成绩和题库读写接口不可用时会提示错误，避免误以为云端数据已经保存。

当前接口：

```text
POST   /api/auth/login
GET    /api/me
GET    /api/platform/{role}/{pageKey}
GET    /api/scores/import-rows
POST   /api/scores/import-rows
DELETE /api/scores/import-rows
GET    /api/questions/import-rows
GET    /api/questions
POST   /api/questions/import-rows
DELETE /api/questions/import-rows
```

后续上云时，把 `TOSAi.Api` 部署到云服务器，再通过 `TOSAI_API_BASE_URL` 指向云端地址即可。数据库接入应放在 `TOSAi.Api` 内部完成，客户端不直接连接数据库。


## 移动端

当前已新增 `TOSAi.Mobile` Flutter 移动端源码骨架，用于学生端和家长端。

```text
TOSAi.Mobile/
  lib/app
  lib/core
  lib/features/auth
  lib/features/shell
  lib/features/shared
```

移动端与 Windows 端共用 `TOSAi.Api`。默认 Android 模拟器访问后端地址：

```text
http://10.0.2.2:5088
```

本机当前没有安装 Flutter，所以暂未执行移动端编译。安装 Flutter 后进入 `TOSAi.Mobile` 目录执行：

```powershell
flutter create --platforms=android,ios .
flutter pub get
flutter run
```

## 成绩导入模板

当前先使用 CSV 作为基础模板，Excel 可以直接打开、编辑、另存为 CSV。字段采用长表结构，便于后续增加学科：

| 字段 | 示例 | 说明 |
| --- | --- | --- |
| 考试名称 | 2026 春季期中考试 | 同一次考试保持一致 |
| 考试日期 | 2026-04-20 | 格式必须是 yyyy-MM-dd |
| 年级 | 初二 | 年级名称 |
| 班级 | 初二 1 班 | 班级名称 |
| 学号 | 20260101 | 学生唯一标识 |
| 姓名 | 李明 | 学生姓名 |
| 学科 | 语文 | 一行一个学生的一门学科成绩 |
| 分数 | 91 | 实得分 |
| 满分 | 120 | 本学科满分 |

在软件的“成绩录入”页面可以导出模板、导入 CSV，并预览导入明细。


## 云端成绩保存

“成绩录入”页面支持把导入后的成绩明细保存到 `TOSAi.Api`。默认未配置数据库时，API 使用进程内内存存储；配置 `DATABASE_URL` 或 `POSTGRES_CONNECTION_STRING` 后会写入 PostgreSQL。

“清空云端数据”会调用 `DELETE /api/scores/import-rows`，真正删除服务端保存的成绩明细。


## 学生成绩趋势

“学生趋势”页面会读取云端保存的成绩明细，并支持：

- 按学生筛选
- 按学科筛选
- 查看考试次数、平均得分率、优势学科、薄弱学科
- 查看每次考试的得分率趋势列表和明细表

使用顺序：

1. 进入“成绩录入”页面。
2. 导入 CSV 成绩模板。
3. 点击“保存云端”。
4. 进入“学生趋势”页面，点击“刷新云端数据”或等待自动读取。

## 添加新页面

1. 在 `Views` 目录新增一个 `UserControl`，例如 `ClassCompareView.xaml` 和 `ClassCompareView.xaml.cs`。
2. 在 `MainWindow.xaml` 左侧导航复制一个 `RadioButton`，修改 `Content` 和 `Tag`。
3. 在 `MainWindow.xaml.cs` 的 `RegisterPages()` 里注册页面：

```csharp
_pages["classCompare"] = new("班级对比", "查看班级之间的成绩差异。", new ClassCompareView());
```

## 接入真实 AI

当前已支持 OpenAI 兼容的 Chat Completions 接口。

使用顺序：

1. 进入“系统设置”。
2. 服务商选择 `OpenAI 兼容接口`、`本地模型服务` 或 `学校统一 AI 平台`。
3. 填写接口地址，例如：

```text
https://api.example.com/v1
```

程序会自动请求：

```text
https://api.example.com/v1/chat/completions
```

如果你填写的地址已经包含 `/chat/completions`，程序会直接使用该地址。

4. 填写模型名称和 API Key。
5. 点击“保存设置”。
6. 进入“AI 分析”，点击“生成分析报告”。

没有配置完整 AI 接口时，程序会自动使用 `MockAiAnalysisService`，保证页面仍然可用。

相关文件：

- `Services/AiSettingsStore.cs`：保存和读取 AI 配置
- `Services/OpenAiCompatibleAnalysisService.cs`：真实 AI 请求实现
- `Views/AnalysisView.xaml.cs`：把本地成绩数据拼成分析上下文

注意：当前为原型阶段，API Key 会保存在 `%LOCALAPPDATA%\TOSAi.TeacherApp\ai-settings.json`。正式部署前建议改为 Windows 凭据管理器或加密存储。






