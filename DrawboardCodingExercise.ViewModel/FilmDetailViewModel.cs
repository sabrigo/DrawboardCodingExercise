using CommunityToolkit.Mvvm.ComponentModel;

using DrawboardCodingExercise.Contracts.CoreFramework;
using DrawboardCodingExercise.Contracts.Services;
using DrawboardCodingExercise.Contracts.StarWars;

using Serilog;

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace DrawboardCodingExercise.ViewModel
{
    public partial class FilmDetailViewModel : BaseViewModel, INavigateToAware, IProvidePageHeader
    {
        private readonly IStarWarsService _starWarsService;
        public FilmDetailViewModel(
            IStarWarsService starWarsService,
            IEventAggregator eventAggregator, 
            IUserInteractionService userInteractionService, 
            ILocalizationService localizationService, 
            ILogger logger) 
            : base(eventAggregator, userInteractionService, localizationService, logger)
        {
            _starWarsService = starWarsService;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReleaseDate))]
        private Film _film;
        public ObservableCollection<string> Characters { get; } = new();

        public string ReleaseDate => Film?.ReleaseDate.ToString("D", CultureInfo.CurrentCulture) ?? string.Empty;

        public string PageHeader => "FilmDetail";

        public Task OnNavigatedToAsync(object parameter)
        {
            if (!(parameter is Film film))
            {
                throw new ArgumentException(
                    $"{nameof(FilmDetailViewModel)} must be navigated to with a {nameof(Film)} parameter",
                    nameof(parameter));
            }

            Film = film;
            Characters.Clear();

            return LoadAsync("Busy.LoadingCharacters", async () =>
            {
                var names = await _starWarsService.GetCharacterNamesAsync(film.CharacterUrls).ConfigureAwait(true);

                Characters.Clear();
                foreach (var name in names)
                {
                    Characters.Add(name);
                }
            });
        }
    }
}
