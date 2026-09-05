using DrawboardCodingExercise.Contracts.StarWars;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrawboardCodingExercise.Services.StarWars
{
    public class StarWarsService : IStarWarsService
    {
        private const string FilmsPath = "api/films";

        private readonly IAPIClient _apiClient;

        public StarWarsService(IAPIClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IReadOnlyList<string>> GetCharacterNamesAsync(IReadOnlyList<string> characterUrls)
        {
            if (characterUrls == null || characterUrls.Count == 0)
            {
                return Array.Empty<string>();
            }

            
            return await Task.WhenAll(characterUrls.Select(GetCharacterNameAsync)).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Film>> GetFilmsAsync()
        {
            var films = await _apiClient.GetAsync<FilmResponse[]>(FilmsPath).ConfigureAwait(false);

            return films?
                       .Where(film => film != null)
                       .OrderBy(film => film.EpisodeId)
                       .Select(Map)
                       .ToList()
                   ?? (IReadOnlyList<Film>)Array.Empty<Film>();
        }

        private async Task<string> GetCharacterNameAsync(string characterUrl)
        {
            var character = await _apiClient.GetAsync<NamedResourceResponse>(ToRelativePath(characterUrl))
                .ConfigureAwait(false);

            return character?.Name;
        }

        private static Film Map(FilmResponse response) => new Film(
        response.Title,
        response.EpisodeId,
        response.OpeningCrawl,
        response.Director,
        response.Producer,
        response.ReleaseDate,
        response.Characters ?? Array.Empty<string>());

        private static string ToRelativePath(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var absolute) ? absolute.AbsolutePath : url;
    }
}
