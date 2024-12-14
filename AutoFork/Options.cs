
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoFork
{
    internal class Options
    {
        [Option("starUser", Required = true, IsSecrets = true, HelpText = "需要查询关注列表的用户")]
        public required string StarListUser { get; set; }

        [Option("forkToken", Required = true, IsSecrets = true, HelpText = "用于 Fork 的用户 token")]
        public required string ForkUserToken { get; set; }

        [Option("enableLog", Default = true, HelpText = "是否启用日志")]
        public bool EnableLog { get; set; }

        [Option("logRepo", Default = "AutoForkLog", HelpText = "日志仓库名")]
        public required string LogRepo { get; set; }

        [Option("minUpdatehHourInterval", Default = 0, HelpText = "最小更新时间间隔（单位：时）")]
        public double MinimumUpdateHourInterval { get; set; }

        public static Options Init()
        {
            var type = typeof(Options);
            var props = type.GetProperties();
            var option = (Options)Activator.CreateInstance(type);
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<OptionAttribute>();
                if (attr == null)
                {
                    throw new InvalidOperationException($"{prop.Name} need ({nameof(OptionAttribute)}.");
                }

                var envValue = Environment.GetEnvironmentVariable(attr.Name);
                Console.WriteLine($"{attr.Name}: [{((attr.IsSecrets && !string.IsNullOrEmpty(envValue)) ? "******" : envValue)}]");
                if (string.IsNullOrEmpty(envValue))
                {
                    if (attr.Required)
                    {
                        throw new ArgumentNullException(attr.Name);
                    }
                    prop.SetValue(option, attr.Default);
                    continue;
                }
                object value = Convert.ChangeType(envValue, prop.PropertyType);
                prop.SetValue(option, value);
            }
            return option;
        }

        private class OptionAttribute : Attribute
        {
            public OptionAttribute(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
            public object Default { get; set; }
            public string HelpText { get; set; }
            public bool Required { get; set; }
            public bool IsSecrets { get; set; }
        }
    }
}
