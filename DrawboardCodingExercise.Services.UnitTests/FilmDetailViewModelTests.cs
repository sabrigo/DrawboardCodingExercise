using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DrawboardCodingExercise.Contracts.Events;
using DrawboardCodingExercise.Contracts.StarWars;
using DrawboardCodingExercise.ViewModel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace DrawboardCodingExercise.Services.UnitTests;

public class FilmDetailViewModelTests
{
    private readonly BaseViewModelTests _baseViewModel = new BaseViewModelTests();

    private static readonly string[] CharacterUrls =
	{
		"https://swapi.info/api/people/1",
		"https://swapi.info/api/people/2"
	};

	private static Film AFilm() => new Film(
		"A New Hope", 4, "It is a period of civil war.", "George Lucas", "Gary Kurtz",
		new DateTimeOffset(1977, 5, 25, 0, 0, 0, TimeSpan.Zero), CharacterUrls);

	private FilmDetailViewModel CreateSubject() => new FilmDetailViewModel(
		_baseViewModel.StarWarsService,
		_baseViewModel.EventAggregator,
		_baseViewModel.UserInteractionService,
		_baseViewModel.LocalizationService,
		_baseViewModel.Logger);

	[Fact]
	public async Task OnNavigatedToAsync_PresentsTheFilmItWasNavigatedWith()
	{
		_baseViewModel.StarWarsService.GetCharacterNamesAsync(Arg.Any<IReadOnlyList<string>>())
			.Returns((IReadOnlyList<string>) new string[0]);
		var film = AFilm();

		var subject = CreateSubject();
		await subject.OnNavigatedToAsync(film);

		subject.Film.ShouldBe(film);
		subject.ReleaseDate.ShouldNotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task OnNavigatedToAsync_LoadsTheCharactersOfThatFilm()
	{
		_baseViewModel.StarWarsService.GetCharacterNamesAsync(CharacterUrls)
			.Returns((IReadOnlyList<string>) new[] { "Luke Skywalker", "C-3PO" });

		var subject = CreateSubject();
		await subject.OnNavigatedToAsync(AFilm());

		subject.Characters.ShouldBe(new[] { "Luke Skywalker", "C-3PO" });
	}

	[Fact]
	public async Task OnNavigatedToAsync_StillShowsTheFilmWhenTheCharactersCannotBeLoaded()
	{
		_baseViewModel.StarWarsService.GetCharacterNamesAsync(Arg.Any<IReadOnlyList<string>>())
			.ThrowsAsync(new InvalidOperationException("boom"));

		var subject = CreateSubject();
		await subject.OnNavigatedToAsync(AFilm());

		subject.Film.ShouldNotBeNull();
		subject.Characters.ShouldBeEmpty();
		subject.HasError.ShouldBeTrue();
		_baseViewModel.EventAggregator.Received(1).Post(new NotifyDoneEvent("Busy.LoadingCharacters"));
	}

	[Fact]
	public async Task OnNavigatedToAsync_FailsLoudlyWhenNavigatedToWithoutAFilm()
	{
		await Should.ThrowAsync<ArgumentException>(() => CreateSubject().OnNavigatedToAsync("not a film"));
	}
}
