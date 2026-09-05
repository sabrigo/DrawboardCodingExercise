using System;
using System.Linq;
using System.Threading.Tasks;

using DrawboardCodingExercise.Services.StarWars;

using NSubstitute;

using Shouldly;

using Xunit;

namespace DrawboardCodingExercise.Services.UnitTests
{
    public class StarWarsServiceTests
    {
        private readonly IAPIClient _apiClient = Substitute.For<IAPIClient>();

        private StarWarsService CreateSubject() => new StarWarsService(_apiClient);

        [Fact]
        public async Task GetFilmsAsync_MapsEveryFieldTheUiPresents()
        {
            _apiClient.GetAsync<FilmResponse[]>("api/films").Returns(new[]
            {
            new FilmResponse
            {
                Title = "A New Hope",
                EpisodeId = 4,
                OpeningCrawl = "It is a period of civil war.",
                Director = "George Lucas",
                Producer = "Gary Kurtz, Rick McCallum",
                ReleaseDate = new DateTimeOffset(1977, 5, 25, 0, 0, 0, TimeSpan.Zero),
                Characters = new[] { "https://swapi.info/api/people/1" }
            }
        });

            var films = await CreateSubject().GetFilmsAsync();

            var film = films.ShouldHaveSingleItem();
            film.Title.ShouldBe("A New Hope");
            film.EpisodeId.ShouldBe(4);
            film.OpeningCrawl.ShouldBe("It is a period of civil war.");
            film.Director.ShouldBe("George Lucas");
            film.Producer.ShouldBe("Gary Kurtz, Rick McCallum");
            film.ReleaseDate.ShouldBe(new DateTimeOffset(1977, 5, 25, 0, 0, 0, TimeSpan.Zero));
            film.CharacterUrls.ShouldBe(new[] { "https://swapi.info/api/people/1" });
        }

        [Fact]
        public async Task GetFilmsAsync_OrdersByEpisodeNumberRatherThanReleaseOrder()
        {
            _apiClient.GetAsync<FilmResponse[]>("api/films").Returns(new[]
            {
            new FilmResponse { Title = "A New Hope", EpisodeId = 4 },
            new FilmResponse { Title = "The Phantom Menace", EpisodeId = 1 },
            new FilmResponse { Title = "Return of the Jedi", EpisodeId = 6 }
        });

            var films = await CreateSubject().GetFilmsAsync();

            films.Select(film => film.EpisodeId).ShouldBe(new[] { 1, 4, 6 });
        }

        [Fact]
        public async Task GetFilmsAsync_ReturnsEmptyWhenTheApiRespondsWithNoBody()
        {
            _apiClient.GetAsync<FilmResponse[]>("api/films").Returns((FilmResponse[])null);

            (await CreateSubject().GetFilmsAsync()).ShouldBeEmpty();
        }

        [Fact]
        public async Task GetFilmsAsync_ToleratesAFilmWithNoCharacters()
        {
            _apiClient.GetAsync<FilmResponse[]>("api/films").Returns(new[]
            {
            new FilmResponse { Title = "A New Hope", EpisodeId = 4, Characters = null }
        });

            var films = await CreateSubject().GetFilmsAsync();

            films.ShouldHaveSingleItem().CharacterUrls.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetCharacterNamesAsync_RequestsThePathRelativeToTheConfiguredServer()
        {
            // SWAPI links with absolute URLs; IAPIClient appends the path to IAPISettings.ServerAddress.
            _apiClient.GetAsync<NamedResourceResponse>("/api/people/1")
                .Returns(new NamedResourceResponse { Name = "Luke Skywalker" });

            var names = await CreateSubject().GetCharacterNamesAsync(new[] { "https://swapi.info/api/people/1" });

            names.ShouldBe(new[] { "Luke Skywalker" });
        }

        [Fact]
        public async Task GetCharacterNamesAsync_KeepsTheOrderOfTheRequestedUrls()
        {
            _apiClient.GetAsync<NamedResourceResponse>("/api/people/1")
                .Returns(new NamedResourceResponse { Name = "Luke Skywalker" });
            _apiClient.GetAsync<NamedResourceResponse>("/api/people/2")
                .Returns(new NamedResourceResponse { Name = "C-3PO" });

            var names = await CreateSubject().GetCharacterNamesAsync(new[]
            {
            "https://swapi.info/api/people/2",
            "https://swapi.info/api/people/1"
        });

            names.ShouldBe(new[] { "C-3PO", "Luke Skywalker" });
        }

        [Fact]
        public async Task GetCharacterNamesAsync_MakesNoCallsWhenTheFilmHasNoCharacters()
        {
            (await CreateSubject().GetCharacterNamesAsync(new string[0])).ShouldBeEmpty();

            await _apiClient.DidNotReceiveWithAnyArgs().GetAsync<NamedResourceResponse>(default);
        }

        [Fact]
        public async Task GetCharacterNamesAsync_MakesNoCallsWhenTheCharacterListIsMissing()
        {
            (await CreateSubject().GetCharacterNamesAsync(null)).ShouldBeEmpty();

            await _apiClient.DidNotReceiveWithAnyArgs().GetAsync<NamedResourceResponse>(default);
        }
    }

}
