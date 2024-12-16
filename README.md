- [English](README.en-US.md) 

- [简体中文](README.md) 

  

# 自动定时 fork 指定用户关注的项目

1. fork 此项目
2. 配置 github secrets

| 名称      | 类型   | 说明                            |
| --------- | ------ | ------------------------------- |
| starUser  | string | **必须** 需要查询关注列表的用户 |
| forkToken | string | **必须** 用于 Fork 的用户 token |
| enableLog              | bool        | **非必须** 是否启用日志，如果不配置则默认为 true             |
| logRepo                | string      | **非必须** 日志仓库名，如果不配置则默认为 AutoForkLog        |
| minUpdatehHourInterval | int         | **非必须** 最小更新时间间隔（单位：时），如果不配置则默认为 0 |
| excluded | string | **非必须** 排除的仓库名（例如 `cyoukon/AutoFork`），支持正则，如果不配置则默认不排除任何仓库 |

3. 修改工作流参数，默认已配置，如无特别要求可不修改（[github 工作流配置文件](./.github/workflows/auto_fork.yml)）

| 节点                                      | 类型        | 说明                                                         |
| ----------------------------------------- | ----------- | ------------------------------------------------------------ |
| on: schedule: cron:                       | cron 表达式 | **必须** 设置自动运行的时间，默认已配置为 每天16:30 UTC (即次日00:30 CST) |

> 项目中所有时间均为 UTC 时间
