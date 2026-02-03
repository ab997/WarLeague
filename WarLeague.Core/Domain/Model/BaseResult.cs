using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Domain.Model
{
    public class BaseResult
    {
        public BaseResult()
        {
            
        }
        public BaseResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
