using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.ImageGenerator
{
    public static class YdkParser
    {
        public static Deck Parse(string text)
        {
            var deck = new Deck();
            var current = deck.Main;

            using var reader = new StringReader(text);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0 || line.StartsWith("#created"))
                    continue;

                switch (line)
                {
                    case "#main":
                        current = deck.Main;
                        continue;

                    case "#extra":
                        current = deck.Extra;
                        continue;

                    case "!side":
                        current = deck.Side;
                        continue;
                }

                current.Add(int.Parse(line));
            }

            return deck;
        }
    }
}
