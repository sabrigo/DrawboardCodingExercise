using DrawboardCodingExercise.Contracts.CoreFramework;
using DrawboardCodingExercise.Contracts.Services;
using DrawboardCodingExercise.Contracts.StarWars;

using NSubstitute;

using Serilog;


namespace DrawboardCodingExercise.Services.UnitTests
{
    public class BaseViewModelTests
    {
        public BaseViewModelTests()
        {
            UserInteractionService.ShowRetryDialogAsync().Returns(RetryDialogResult.Cancel);

            LocalizationService.Translate(Arg.Any<string>(), Arg.Any<object[]>())
                .Returns(call => call.ArgAt<string>(0));
        }

        public IStarWarsService StarWarsService { get; } = Substitute.For<IStarWarsService>();
        public INavigationService NavigationService { get; } = Substitute.For<INavigationService>();
        public IEventAggregator EventAggregator { get; } = Substitute.For<IEventAggregator>();
        public IUserInteractionService UserInteractionService { get; } = Substitute.For<IUserInteractionService>();
        public ILocalizationService LocalizationService { get; } = Substitute.For<ILocalizationService>();
        public ILogger Logger { get; } = Substitute.For<ILogger>();
    }
}
