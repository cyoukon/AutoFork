通过 GitHub Actions 运行 .NET 控制台项目涉及创建一个工作流文件，该文件定义了如何构建和运行您的项目。GitHub Actions 是一种持续集成和持续部署（CI/CD）服务，允许您自动化软件开发工作流程。

以下是一个简单的例子，展示了如何配置 GitHub Actions 来构建和运行一个 .NET 控制台应用程序。我们将创建一个名为 .github/workflows/dotnet.yml 的 YAML 文件，在项目的根目录下。

```yaml
name: Build and Test .NET Console App

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build:

    runs-on: ubuntu-latest

    steps:
    - name: Checkout repository
      uses: actions/checkout@v3

    - name: Setup .NET SDK
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: '7.0.x' # 使用您需要的 .NET 版本

    - name: Restore dependencies
      run: dotnet restore

    - name: Build the project
      run: dotnet build --configuration Release --no-restore

    - name: Run tests
      run: dotnet test --no-build --verbosity normal

    - name: Run the application
      run: dotnet run --project ./path/to/your/project.csproj --configuration Release
```
上述配置中：

name: 定义工作流的名称。
on: 触发此工作流的事件，这里设定为推送到 main 分支或对 main 分支发起拉取请求时触发。
jobs: 工作流中定义的一系列任务，每个任务都是一个独立的作业。这里只有一个名为 build 的作业。
runs-on: 指定作业将在哪种类型的虚拟环境中运行。ubuntu-latest 表示最新的 Ubuntu 环境。
steps: 作业中的具体步骤，按顺序执行。
Checkout repository: 检出代码库到 runner 上。
Setup .NET SDK: 设置指定版本的 .NET SDK。
Restore dependencies: 恢复项目依赖。
Build the project: 构建项目。
Run tests: 如果有测试项目，则运行它们。
Run the application: 运行控制台应用程序。请根据实际情况调整路径和参数。
确保将 ./path/to/your/project.csproj 替换为您的 .NET 项目文件的实际路径，并且如果您没有测试项目，可以省略 Run tests 步骤。

这个工作流将会在每次推送代码到 main 分支或者当有人针对 main 分支发起拉取请求时自动触发。它会设置 .NET SDK、恢复依赖项、构建项目、运行任何单元测试（如果有），最后运行控制台应用程序。如果构建或测试失败，GitHub Actions 将报告错误。