using System;
using System.Collections.Generic;
using System.Text;

namespace DrawboardCodingExercise.Contracts.StarWars
{
    public record Film(
    string Title,
    int EpisodeId,
    string OpeningCrawl,
    string Director,
    string Producer,
    DateTimeOffset ReleaseDate,
    IReadOnlyList<string> CharacterUrls);
}
