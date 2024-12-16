
using System.Text.RegularExpressions;

namespace AutoFork
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Run(Options.Init());
        }

        private static async Task Run(Options option)
        {
            await using var githubClient = new GithubApiClient(option);

            var history = await githubClient.GetHistoryAsync() ?? [];

            var starredRepos = await githubClient.GetStarredReposAsync(option.StarListUser);

            var count = 0;
            var skipCount = 0;
            var now = DateTime.Now;
            try
            {
                foreach (var (key, value) in starredRepos)
                {
                    count++;

                    history.TryGetValue(key, out var historyModel);

                    if (!string.IsNullOrEmpty(option.ExcludedRepo) && Regex.IsMatch(key, option.ExcludedRepo))
                    {
                        githubClient.WriteLog($"第{count}个仓库命中排除规则，" +
                            $"无需 Fork：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
                    }

                    if (historyModel != null)
                    {
                        if (historyModel.ForkedAt >= value)
                        {
                            githubClient.WriteLog($"第{count}个仓库未更新，" +
                                $"无需 Fork：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
                            skipCount++;
                            continue;
                        }

                        if (historyModel.ForkedAt.AddHours(option.MinimumUpdateHourInterval) > now)
                        {
                            githubClient.WriteLog($"第{count}个仓库距上次 Fork 间隔时间为 {(now - historyModel.ForkedAt).TotalHours} 小时，" +
                                $"小于 {option.MinimumUpdateHourInterval} 小时，" +
                                $"无需 Fork：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
                            skipCount++;
                            continue;
                        }
                    }

                    await githubClient.ForkRepositoryAsync(key);
                    githubClient.WriteLog(
                        $"第{count}个仓库 Fork 完成：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
                    history[key] = new RespModels.HistoryModel
                    {
                        RepoFullName = key,
                        UpdatedAt = value,
                        ForkedAt = DateTime.UtcNow,
                    };
                }
            }
            finally
            {
                githubClient.WriteLog($"执行完毕，共 {count} 个仓库，其中 {skipCount} 无需 Fork");
                githubClient.WriteLog("done!");
                await githubClient.UpdateHistoryAsync(history);
            }
        }
    }
}
