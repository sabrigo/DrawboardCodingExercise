using System;
using DrawboardCodingExercise.Contracts.CoreFramework;
using DrawboardCodingExercise.Contracts.Events;
using DrawboardCodingExercise.Contracts.Services;
using DrawboardCodingExercise.ViewModel;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DrawboardCodingExercise.Services.UnitTests
{
	public class ShellViewModelTests
	{
		private readonly INavigationService _navigationService = Substitute.For<INavigationService>();
		private readonly IEventAggregator _eventAggregator = Substitute.For<IEventAggregator>();

		/// <summary>
		/// Captures the busy/done handlers the shell subscribes with, so they can be invoked directly.
		/// </summary>
		private (ShellViewModel Shell, Action<NotifyBusyEvent> Busy, Action<NotifyDoneEvent> Done) CreateSubject()
		{
			Action<NotifyBusyEvent> busy = null;
			Action<NotifyDoneEvent> done = null;

			_eventAggregator.SubscribeOnUI(Arg.Do<Action<NotifyBusyEvent>>(handler => busy = handler))
				.Returns(Substitute.For<IDisposable>());
			_eventAggregator.SubscribeOnUI(Arg.Do<Action<NotifyDoneEvent>>(handler => done = handler))
				.Returns(Substitute.For<IDisposable>());

			var shell = new ShellViewModel(_navigationService, _eventAggregator);
			_ = shell.OnNavigatedToAsync(null);

			return (shell, busy, done);
		}

		[Fact]
		public void NotifyBusy_ShowsTheOperationInProgress()
		{
			var (shell, busy, _) = CreateSubject();

			busy(new NotifyBusyEvent("Loading films"));

			shell.IsBusy.ShouldBeTrue();
			shell.ThingInProgress.ShouldBe("Loading films");
		}

		[Fact]
		public void NotifyDone_ClearsTheOperationItMatches()
		{
			var (shell, busy, done) = CreateSubject();

			busy(new NotifyBusyEvent("Loading films"));
			done(new NotifyDoneEvent("Loading films"));

			shell.IsBusy.ShouldBeFalse();
			shell.ThingInProgress.ShouldBeNull();
		}

		[Fact]
		public void NotifyDone_IgnoresAnEventItNeverSawStart()
		{
			// Regression: this used to call RemoveAt(-1) and take the whole shell down.
			var (shell, busy, done) = CreateSubject();
			busy(new NotifyBusyEvent("Loading films"));

			Should.NotThrow(() => done(new NotifyDoneEvent("Something else entirely")));

			shell.IsBusy.ShouldBeTrue();
		}

		[Fact]
		public void Dispose_StopsListeningForNavigation()
		{
			var (shell, _, _) = CreateSubject();

			shell.Dispose();

			_navigationService.Received(1).Navigated -= Arg.Any<Action>();
		}
	}
}