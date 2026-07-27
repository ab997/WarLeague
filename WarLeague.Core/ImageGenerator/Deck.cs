using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.ImageGenerator
{
    public sealed class Deck
    {
        public List<int> Main { get; } = new();
        public List<int> Extra { get; } = new();
        public List<int> Side { get; } = new();
    }
}
