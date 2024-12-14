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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoFork
{
    internal class GithubApiClient : IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Options _options;
        private string _userName;
        private string _historySha;
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

        public async Task<Dictionary<string, DateTime>> GetStarredReposAsync(string user = null)
        {
            try
            {
                var repos = new Dictionary<string, DateTime>();
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
                    _ = data.Select(d => repos[d.full_name] = d.updated_at).ToArray();
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
                var respBody = await resp.Content.ReadAsStringAsync();
                WriteLog(respBody);
                if (resp.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new HttpRequestException(message);
                }
            }
            WriteLog(message);
        }

        private string CombineGetUrl(string url, Dictionary<string, object> queryParameters)
        {
            return $"{url}?{string.Join('&', queryParameters.Select(p => $"{p.Key}={p.Value}"))}";
        }

        public void WriteLog(string msg, Exception ex = null)
        {
            Console.WriteLine(msg);
            _log.Add($"【{DateTime.UtcNow:HH-mm-ss_ffff}】{msg}");
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
            string userName = await GetCurrentUserNameAsync();

            var now = DateTime.UtcNow;
            var url = $"repos/{userName}/{_options.LogRepo}/contents/Logs{now:yyyyMM}/{now:yyyy-MM-dd_HH-mm-ss}.log";
            var content = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, _log)));
            var logResp = await _httpClient.PutAsync(url, JsonContent.Create(new
            {
                message = $"[{now:yyyy-MM-dd_HH-mm-ss}] auto commit log by github action.",
                content
            }));
            Console.WriteLine($"PUT {url}");
            Console.WriteLine(logResp.StatusCode);
            logResp.EnsureSuccessStatusCode();
        }

        private async Task<string> GetCurrentUserNameAsync()
        {
            if (!string.IsNullOrWhiteSpace(_userName))
            {
                return _userName;
            }

            var userResp = await _httpClient.GetAsync("user");
            string userRespBody = await userResp.Content.ReadAsStringAsync();
            if (!userResp.IsSuccessStatusCode)
            {
                WriteLog($"GET: user{Environment.NewLine}{userResp.StatusCode}{Environment.NewLine}{userRespBody}");
                throw new HttpRequestException(userResp.StatusCode.ToString());
            }
            var userName = JsonNode.Parse(userRespBody)?["login"]?.ToString();
            _userName = userName;
            return userName;
        }

        public async Task<Dictionary<string, HistoryModel>> GetHistoryAsync()
        {
            if (!_options.EnableLog)
            {
                return null;
            }
            var url = $"repos/{await GetCurrentUserNameAsync()}/{_options.LogRepo}/contents/history.json";
            try
            {
                var readmeJson = await _httpClient.GetStringAsync(url);
                var jNode = JsonNode.Parse(readmeJson);
                _historySha = jNode["sha"].ToString();
                var readme = jNode["content"].ToString();
                var readmeDecoded = Encoding.UTF8.GetString(Convert.FromBase64String(readme));
                var history = JsonSerializer.Deserialize<Dictionary<string, HistoryModel>>(readmeDecoded);
                return history;
            }
            catch (Exception ex)
            {
                WriteLog(url, ex);
                return null;
            }
        }

        public async Task UpdateHistoryAsync(Dictionary<string, HistoryModel> history)
        {
            if (!_options.EnableLog)
            {
                return;
            }
            var createRepoUrl = "user/repos";
            var createRepoResp = await _httpClient.PostAsync(createRepoUrl, JsonContent.Create(new
            {
                name = _options.LogRepo,
                visibility = "private",
                @private = true
            }));
            Console.WriteLine($"POST: {createRepoUrl}");
            Console.WriteLine(createRepoResp.StatusCode.ToString());

            var hsitoryJson = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var content = Convert.ToBase64String(Encoding.UTF8.GetBytes(hsitoryJson));

            var url = $"repos/{await GetCurrentUserNameAsync()}/{_options.LogRepo}/contents/history.json";
            var resp = await _httpClient.PutAsync(url, JsonContent.Create(new
            {
                message = $"[{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}] update readme log.",
                content,
                sha = _historySha
            }));
            WriteLog($"PUT {url}{Environment.NewLine}{resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                WriteLog(await resp.Content.ReadAsStringAsync());
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CommitLog();
            _httpClient.Dispose();
        }
    }
}
