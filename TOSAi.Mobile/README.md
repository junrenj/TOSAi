# TOSAi.Mobile

Flutter 移动端原型，当前包含学生端和家长端。页面共用同一套组件，保证 Android 和 iOS 的布局与视觉一致。

## 当前功能

- 角色选择：学生端 / 家长端
- 学生端：首页、作业、进度、拍照搜题入口
- 家长端：首页、趋势、报告、心理关注
- 优先请求 `TOSAi.Api`
- API 不可用时自动回退到移动端内置模拟数据

## API 地址

默认移动端 API 地址：

```text
http://10.0.2.2:5088
```

这是 Android 模拟器访问 Windows 宿主机的地址。如果是 iOS 模拟器，可以用：

```powershell
flutter run --dart-define=TOSAI_API_BASE_URL=http://localhost:5088
```

如果是真机，需要把地址改成电脑在局域网内的 IP，例如：

```powershell
flutter run --dart-define=TOSAI_API_BASE_URL=http://192.168.1.23:5088
```

## 第一次生成平台目录

当前仓库里先放了 Flutter 的 `lib/` 源码和 `pubspec.yaml`。安装 Flutter 后，在 `TOSAi.Mobile` 目录执行：

```powershell
flutter create --platforms=android,ios .
flutter pub get
```

## 运行

先启动后端：

```powershell
dotnet run --project ..\TOSAi.Api\TOSAi.Api.csproj --urls http://localhost:5088
```

再运行移动端：

```powershell
flutter run
```

## 目录

```text
lib/
  app/                  App 根节点
  core/api/             API 客户端、Mock 回退
  core/models/          DTO 模型
  core/theme/           主题
  features/auth/        角色选择
  features/shell/       底部导航框架
  features/shared/      共用页面组件
```
