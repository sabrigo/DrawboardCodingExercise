using System.Linq;
using System.Threading.Tasks;
using DrawboardCodingExercise.Services.StarWars;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Serilog.Core;
using Shouldly;
using Xunit;

namespace DrawboardCodingExercise.Services.IntegrationTests
{
	/// <summary>
	/// Exercises the real SWAPI service. These need network access, and will fail if swapi.info is down.
	/// </summary>
	[Trait("Category", "Integration")]
	public class SwapiIntegrationTests
	{
		private static StarWarsService CreateSubject()
		{
			var settings = Substitute.For<IAPISettings>();
			settings.ServerAddress.Returns("https://swapi.info");

			// The same serializer configuration the app registers in WebServicesModule.
			var jsonSettings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				Formatting = Formatting.Indented
			};

			return new StarWarsService(new APIClient(settings, jsonSettings, Logger.None));
		}

		[Fact]
		public async Task GetFilmsAsync_ReturnsTheSixNumberedEpisodesWithTheirDetails()
		{
			var films = await CreateSubject().GetFilmsAsync();

			films.Count.ShouldBeGreaterThanOrEqualTo(6);
			films.Select(film => film.EpisodeId).ShouldBe(films.Select(film => film.EpisodeId).OrderBy(id => id));

			var newHope = films.First(film => film.EpisodeId == 4);
			newHope.Title.ShouldBe("A New Hope");
			newHope.Director.ShouldBe("George Lucas");
			newHope.ReleaseDate.Year.ShouldBe(1977);
			newHope.OpeningCrawl.ShouldContain("It is a period of civil war");
			newHope.CharacterUrls.ShouldNotBeEmpty();
		}

		[Fact]
		public async Task GetCharacterNamesAsync_ResolvesTheAbsoluteUrlsTheFilmsEndpointReturns()
		{
			var subject = CreateSubject();
			var newHope = (await subject.GetFilmsAsync()).First(film => film.EpisodeId == 4);

			var names = await subject.GetCharacterNamesAsync(newHope.CharacterUrls);

			names.Count.ShouldBe(newHope.CharacterUrls.Count);
			names.ShouldAllBe(name => !string.IsNullOrWhiteSpace(name));
			names.ShouldContain("Luke Skywalker");
		}
	}
}