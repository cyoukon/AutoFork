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
            foreach (var repo in starredRepos)
            {
                await githubClient.ForkRepositoryAsync(repo);
            }
        }
    }
}
