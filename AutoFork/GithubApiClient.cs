using AutoFork.RespModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoFork
{
    internal class GithubApiClient : IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Options _options;
        private readonly List<string> _log;

        public GithubApiClient(Options options)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com"),
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ForkUserToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "auto-fork");
            _options = options;
            _log = [];
            WriteLog("开始执行");
        }

        public async Task<List<string>> GetStarredReposAsync(string user = null)
        {
            try
            {
                var repos = new List<string>();
                string url;
                if (user == null)
                {
                    url = $"user/starred";
                }
                else
                {
                    url = $"users/{user}/starred";
                }
                int pageSize = 100;
                int page = 1;
                while (true)
                {
                    var paras = new Dictionary<string, object>
                {
                    { "per_page", pageSize },
                    { "page", page++},
                    { "sort", "updated" },
                    { "direction", "desc" },
                };
                    var fullUrl = CombineGetUrl(url, paras);

                    // 发送GET请求并获取响应
                    var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string message = $"GET: {fullUrl}{Environment.NewLine}{response.StatusCode}{Environment.NewLine}{responseBody}";
                        WriteLog(message);
                        throw new HttpRequestException(message);
                    }

                    // 反序列化JSON数据到C#对象
                    var data = JsonSerializer.Deserialize<ListRepositortyModel[]>(responseBody);
                    if (data == null)
                    {
                        break;
                    }
                    repos.AddRange(data.Select(r => r.full_name).ToList());
                    if (data.Length < pageSize)
                    {
                        break;
                    }
                }
                return repos;
            }
            catch (Exception ex)
            {
                WriteLog($"获取仓库标星时异常。", ex);
                throw;
            }
        }

        public async Task ForkRepositoryAsync(string repoFullName)
        {
            var url = $"repos/{repoFullName}/forks";
            HttpResponseMessage resp;
            try
            {
                resp = await _httpClient.PostAsync(url, JsonContent.Create(new
                {
                    name = repoFullName.Replace('/', '.'),
                    default_branch_only = false
                }));
            }
            catch (Exception ex)
            {
                WriteLog($"克隆仓库时异常：{repoFullName}。", ex);
                throw;
            }
            string message = $"POST: {url}{Environment.NewLine}{resp.StatusCode}";
            if (resp.StatusCode != HttpStatusCode.Accepted)
            {
                WriteLog(message);
                throw new HttpRequestException(message);
            }
            WriteLog(message);
        }

        private string CombineGetUrl(string url, Dictionary<string, object> queryParameters)
        {
            return $"{url}?{string.Join('&', queryParameters.Select(p => $"{p.Key}={p.Value}"))}";
        }

        private void WriteLog(string msg, Exception ex = null)
        {
            Console.WriteLine(msg);
            _log.Add($"【{DateTime.Now:HH-mm-ss_ffff}】{msg}");
            if (ex != null)
            {
                _log.Add(ex.Message);
                _log.Add(ex.ToString());
                lock (_log)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }
            }
        }

        private async Task CommitLog()
        {
            if (!_options.EnableLog)
            {
                return;
            }
            var userResp = await _httpClient.GetAsync("user");
            string userRespBody = await userResp.Content.ReadAsStringAsync();
            if (!userResp.IsSuccessStatusCode)
            {
                Console.WriteLine("GET: user");
                Console.WriteLine(userRespBody);
                throw new HttpRequestException(userResp.StatusCode.ToString());
            }
            var userName = JsonNode.Parse(userRespBody)?["login"]?.ToString();

            var createRepoUrl = "user/repos";
            var createRepoResp = await _httpClient.PostAsync(createRepoUrl, JsonContent.Create(new
            {
                name = _options.LogRepo
            }));
            Console.WriteLine($"POST: {createRepoUrl}");
            Console.WriteLine(createRepoResp.StatusCode.ToString());

            var now = DateTime.Now;
            var url = $"repos/{userName}/{_options.LogRepo}/contents/Logs{now:yyyyMM}/{now:yyyy-MM-dd_HH-mm-ss}.log";
            var content = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, _log)));
            var logResp = await _httpClient.PutAsync(url, JsonContent.Create(new
            {
                message = $"[{now:yyyy-MM-dd_HH-mm-ss}] auto commit log by github action.",
                content,
                visibility = "private",
                @private = true
            }));
            Console.WriteLine($"PUT {url}");
            Console.WriteLine(logResp.StatusCode);
            logResp.EnsureSuccessStatusCode();
        }

        public async ValueTask DisposeAsync()
        {
            await CommitLog();
            _httpClient.Dispose();
        }
    }
}
