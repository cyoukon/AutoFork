using CommandLine;

namespace AutoFork
{
    internal class Options
    {
        [Option("starUser", Required = true, HelpText = "需要查询关注列表的用户")]
        public required string StarListUser { get; set; }

        [Option("forkToken", Required = true, HelpText = "用于 Fork 的用户 token")]
        public required string ForkUserToken { get; set; }

        [Option("enableLog", Default = true, HelpText = "是否启用日志")]
        public bool EnableLog { get; set; }

        [Option("logRepo", Default = "AutoForkLog", HelpText = "日志仓库名")]
        public required string LogRepo { get; set; }

        [Option("minUpdatehHourInterval", Default = 0, HelpText = "最小更新时间间隔（单位：时）")]
        public double MinimumUpdateHourInterval { get; set; }
    }
}
