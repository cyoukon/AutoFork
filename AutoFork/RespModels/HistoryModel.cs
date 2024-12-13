using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFork.RespModels
{
    internal class HistoryModel
    {
        public string RepoFullName { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime ForkedAt { get; set; }
    }
}
