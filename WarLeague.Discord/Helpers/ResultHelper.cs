using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Domain.Model;

namespace WarLeague.Discord.Helpers
{
    public static class ResultHelper
    {
        public static string Stringify(params BaseResult[] results)
        {
            if (results == null || results.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var result in results)
            {
                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    sb.AppendLine(result.Message);
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
