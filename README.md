# 自动定时 fork 指定用户关注的项目

1. fork 此项目
2. 配置 github secrets

| 名称                   | 类型   | Description                                                  |
| ---------------------- | ------ | ------------------------------------------------------------ |
| starUser               | string | **必须** 需要查询关注列表的用户                              |
| forkToken              | string | **必须** 用于 Fork 的用户 token                              |
| enableLog              | bool   | **非必须** 是否启用日志，默认为 true                         |
| logRepo                | string | **非必须** 日志仓库名，默认为 AutoForkLog。需要 forkToken 有读写该仓库的权限。如果 forkToken 有创建仓库的权限，则不需要手动创建该仓库 |
| minUpdatehHourInterval | double | **非必须** 最小更新时间间隔（单位：时），默认为 0            |

3. 默认每天 UTC 时间 16：30 运行一次，可修改以下文件第 8 行的 cron 表达式自行调整

[github 工作流配置文件](./.github/workflows/auto_fork.yml)

