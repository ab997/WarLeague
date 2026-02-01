using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Domain.Model
{
    public readonly record struct Result(bool Success, string Message);
}
