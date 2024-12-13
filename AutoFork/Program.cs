using CommandLine;

namespace AutoFork
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var parser = Parser.Default.ParseArguments<Options>(args);
            await parser.WithParsedAsync(Run);
            await parser.WithNotParsedAsync(OnPraseError);
        }

        private static Task OnPraseError(IEnumerable<Error> errors)
        {
            return Task.CompletedTask;
        }

        private static async Task Run(Options option)
        {
            await using var githubClient = new GithubApiClient(option);

            var history = await githubClient.GetHistoryAsync() ?? new Dictionary<string, RespModels.HistoryModel>();

            var starredRepos = await githubClient.GetStarredReposAsync(option.StarListUser);

            var count = 0;
            var skipCount = 0;
            try
            {
                foreach (var (key, value) in starredRepos)
                {
                    count++;
                    history.TryGetValue(key, out var historyModel);

                    if (historyModel != null && historyModel.ForkedAt >= value)
                    {
                        Console.WriteLine($"第{count}个仓库未更新，无需 Fork：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
                        skipCount++;
                        continue;
                    }

                    await githubClient.ForkRepositoryAsync(key);
                    Console.WriteLine($"第{count}个仓库 Fork 完成：{key}。仓库更新时间：{value}，上次 Fork 时间：{historyModel?.ForkedAt}");
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
                Console.WriteLine($"执行完毕，共 {count} 个仓库，其中 {skipCount} 无需 Fork");
                Console.WriteLine("done!");
                await githubClient.UpdateHistoryAsync(history);
            }
        }
    }
}
