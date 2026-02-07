using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;

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

        public static string Stringify(params object[] items)
        {
            if (items == null || items.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                if (item is BaseResult result)
                {
                    if (!string.IsNullOrWhiteSpace(result.Message))
                    {
                        sb.AppendLine(result.Message);
                    }
                }
                else if (item is string str)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        sb.AppendLine(str);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
