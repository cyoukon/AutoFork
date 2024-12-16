- [English](README.en-US.md) 

- [简体中文](README.md) 

  

# Automatically schedule to fork repositories that a specified user starred.

1. fork this repositories
2. Configure GitHub Secrets

| Name             | Type    | Description                                                |
| ---------------- | ------- | ---------------------------------------------------------- |
| starUser         | string  | **Required** The user whose star list needs to be queried  |
| forkToken        | string  | **Required** User token used for Forking                    |
| enableLog        | bool    | **Optional** Whether to enable logs; defaults to true if not configured |
| logRepo          | string  | **Optional** Log repository name; defaults to AutoForkLog if not configured |
| minUpdateHourInterval | int   | **Optional** Minimum update interval (in hours); defaults to 0 if not configured |
| excluded         | string  | **Optional** Excluded repository names (e.g., `cyoukon/AutoFork`), supports regex; defaults to excluding no repositories if not configured |

3. Modify workflow parameters, which are configured by default. No modification is required unless you have special requirements ([GitHub Workflow Configuration File](./.github/workflows/auto_fork.yml))

| Node                                        | Type           | Description                                                   |
| ------------------------------------------- | -------------- | ------------------------------------------------------------- |
| on: schedule: cron:                         | cron expression| **Required** Set the automatic run time; it's configured by default to 16:30 UTC daily (which is 00:30 CST the next day) |

> All times in the project are in UTC
