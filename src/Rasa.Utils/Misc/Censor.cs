using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Misc
{
    // Modified version of http://james.newtonking.com/archive/2009/07/03/simple-net-profanity-filter
    public class Censor
    {
        public IList<string> CensoredWords { get; private set; }

        public Censor(IEnumerable<string> censoredWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException(nameof(censoredWords));

            CensoredWords = new List<string>(censoredWords);
        }

        public bool ContainsProfanity(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            bool containsProfanity = false;
            string censoredText = text;

            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);

                containsProfanity = Regex.IsMatch(censoredText, regularExpression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                if (containsProfanity)
                    break;
            }

            return containsProfanity;
        }

        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;

            return new string('*', word.Length);
        }

        private string ToRegexPattern(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);

            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern;
            }

            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }
    }
}
