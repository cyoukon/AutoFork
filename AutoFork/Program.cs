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
            var starredRepos = await githubClient.GetStarredReposAsync(option.StarListUser);
            for (int i = 0; i < starredRepos.Count; i++)
            {
                string repo = starredRepos[i];
                await githubClient.ForkRepositoryAsync(repo);
                Console.WriteLine($"第{i + 1}个仓库 Fork 完成：{repo}");
            }
            Console.WriteLine("done!");
        }
    }
}
