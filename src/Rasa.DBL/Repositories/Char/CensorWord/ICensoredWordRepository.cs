using System.Collections.Generic;

namespace Rasa.Repositories.Char.CensorWord
{
    using Structures.Char;

    public interface ICensoredWordRepository
    {
        List<string> GetCensoredWords();
    }
}
