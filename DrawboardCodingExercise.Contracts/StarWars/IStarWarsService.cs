using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DrawboardCodingExercise.Contracts.StarWars
{
    public interface IStarWarsService
    {
        Task<IReadOnlyList<Film>> GetFilmsAsync();

        Task<IReadOnlyList<string>> GetCharacterNamesAsync(IReadOnlyList<string> characterUrls);
    }
}
