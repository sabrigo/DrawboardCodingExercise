using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DrawboardCodingExercise.Contracts;
using DrawboardCodingExercise.Contracts.Events;
using DrawboardCodingExercise.Contracts.Services;
using DrawboardCodingExercise.Contracts.StarWars;
using DrawboardCodingExercise.ViewModel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace DrawboardCodingExercise.Services.UnitTests
{
	public class FilmsViewModelTests
	{
		private readonly BaseViewModelTests _baseviewModel = new BaseViewModelTests();

		private static Film AFilm(int episodeId = 4, string title = "A New Hope") =>
			new Film(title, episodeId, "crawl", "director", "producer", DateTimeOffset.UnixEpoch, new string[0]);

		private FilmsViewModel CreateSubject() => new FilmsViewModel(
			_baseviewModel.StarWarsService,
			_baseviewModel.NavigationService,
			_baseviewModel.EventAggregator,
			_baseviewModel.UserInteractionService,
			_baseviewModel.LocalizationService,
			_baseviewModel.Logger);

		[Fact]
		public async Task OnNavigatedToAsync_PresentsTheFilmsFromTheService()
		{
			_baseviewModel.StarWarsService.GetFilmsAsync()
				.Returns((IReadOnlyList<Film>)new[] { AFilm(1, "The Phantom Menace"), AFilm(4) });

			var subject = CreateSubject();
			await subject.OnNavigatedToAsync(null);

			subject.Films.Select(film => film.Title).ShouldBe(new[] { "The Phantom Menace", "A New Hope" });
			subject.HasError.ShouldBeFalse();
		}

		[Fact]
		public async Task OnNavigatedToAsync_ReportsBusyForTheDurationOfTheLoad()
		{
			_baseviewModel.StarWarsService.GetFilmsAsync().Returns((IReadOnlyList<Film>)new Film[0]);

			await CreateSubject().OnNavigatedToAsync(null);

			_baseviewModel.EventAggregator.Received(1).Post(new NotifyBusyEvent("Busy.LoadingFilms"));
			_baseviewModel.EventAggregator.Received(1).Post(new NotifyDoneEvent("Busy.LoadingFilms"));
		}

		[Fact]
		public async Task OnNavigatedToAsync_ReportsDoneEvenWhenTheLoadFails()
		{
			_baseviewModel.StarWarsService.GetFilmsAsync().ThrowsAsync(new InvalidOperationException("boom"));

			await CreateSubject().OnNavigatedToAsync(null);

			// Otherwise the shell's progress indicator would spin forever.
			_baseviewModel.EventAggregator.Received(1).Post(new NotifyDoneEvent("Busy.LoadingFilms"));
		}

		[Fact]
		public async Task OnNavigatedToAsync_SurfacesAnErrorAndStopsWhenTheUserCancelsTheRetry()
		{
			_baseviewModel.StarWarsService.GetFilmsAsync().ThrowsAsync(new InvalidOperationException("boom"));

			var subject = CreateSubject();
			await subject.OnNavigatedToAsync(null);

			subject.HasError.ShouldBeTrue();
			subject.ErrorMessage.ShouldBe("Errors.LoadFailed");
			await _baseviewModel.StarWarsService.Received(1).GetFilmsAsync();
		}

		[Fact]
		public async Task OnNavigatedToAsync_RetriesTheLoadWhenTheUserAsksToAndClearsTheError()
		{
			var attempts = 0;
			_baseviewModel.StarWarsService.GetFilmsAsync().Returns(_ =>
			{
				if (++attempts == 1)
				{
					throw new InvalidOperationException("boom");
				}

				return Task.FromResult<IReadOnlyList<Film>>(new[] { AFilm() });
			});
			_baseviewModel.UserInteractionService.ShowRetryDialogAsync().Returns(RetryDialogResult.Retry);

			var subject = CreateSubject();
			await subject.OnNavigatedToAsync(null);

			attempts.ShouldBe(2);
			subject.Films.ShouldHaveSingleItem().Title.ShouldBe("A New Hope");
			subject.HasError.ShouldBeFalse();
		}

		[Fact]
		public async Task OnNavigatedToAsync_ReplacesRatherThanAppendsWhenLoadedTwice()
		{
			_baseviewModel.StarWarsService.GetFilmsAsync().Returns((IReadOnlyList<Film>)new[] { AFilm() });

			var subject = CreateSubject();
			await subject.OnNavigatedToAsync(null);
			await subject.OnNavigatedToAsync(null);

			subject.Films.Count.ShouldBe(1);
		}

		[Fact]
		public async Task SelectFilmCommand_NavigatesToTheDetailPageWithTheChosenFilm()
		{
			var film = AFilm();

			await CreateSubject().SelectFilmCommand.ExecuteAsync(film);

			await _baseviewModel.NavigationService.Received(1).NavigateAsync(PageKey.FilmDetail, film);
		}
	}
}