using CommunityToolkit.Mvvm.ComponentModel;

using DrawboardCodingExercise.Contracts.Events;
using DrawboardCodingExercise.Contracts.Services;

using Serilog;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DrawboardCodingExercise.ViewModel
{
    public abstract partial class BaseViewModel: ObservableObject
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserInteractionService _userInteractionService;
        private readonly ILogger _logger;

        protected BaseViewModel(
            IEventAggregator eventAggregator,
            IUserInteractionService userInteractionService,
            ILocalizationService localizationService,
            ILogger logger)
        {
            _eventAggregator = eventAggregator;
            _userInteractionService = userInteractionService;
            _logger = logger;
            Localization = localizationService;
        }

        protected ILocalizationService Localization { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? _errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        protected async Task LoadAsync(string busyTextKey, Func<Task> load)
        {
            var busyText = Localization.Translate(busyTextKey);

            while (true)
            {
                ErrorMessage = null;
                _eventAggregator.Post(new NotifyBusyEvent(busyText));

                try
                {
                    await load().ConfigureAwait(true);
                    return;
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Failed to load {ViewModel}", GetType().Name);
                    ErrorMessage = Localization.Translate("Errors.LoadFailed");
                }
                finally
                {
                    // Posted in the finally so the busy indicator is always cleared, including while the
                    // retry dialog is up.
                    _eventAggregator.Post(new NotifyDoneEvent(busyText));
                }

                var retry = await _userInteractionService.ShowRetryDialogAsync().ConfigureAwait(true);
                if (retry == RetryDialogResult.Cancel)
                {
                    return;
                }
            }
        }
    }
}
