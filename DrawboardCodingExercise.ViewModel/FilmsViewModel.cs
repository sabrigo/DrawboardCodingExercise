using CommunityToolkit.Mvvm.Input;

using DrawboardCodingExercise.Contracts;
using DrawboardCodingExercise.Contracts.CoreFramework;
using DrawboardCodingExercise.Contracts.Services;
using DrawboardCodingExercise.Contracts.StarWars;

using Serilog;

using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DrawboardCodingExercise.ViewModel
{
    public partial class FilmsViewModel : BaseViewModel, INavigateToAware, IProvidePageHeader
    {
        private readonly IStarWarsService _starWarsService;
        private readonly INavigationService _navigationService;

        public FilmsViewModel(IStarWarsService starWarsService,
            INavigationService navigationService,
            IEventAggregator eventAggregator, 
            IUserInteractionService userInteractionService, 
            ILocalizationService localizationService, 
            ILogger logger) 
            : base(eventAggregator, userInteractionService, localizationService, logger)
        {
            _starWarsService = starWarsService;
            _navigationService = navigationService;
        }

        public ObservableCollection<Film> Films { get; } = new();

        public string PageHeader => "Films";

        public Task OnNavigatedToAsync(object parameter) => LoadAsync("Busy.LoadingFilms", async () =>
        {
            var films = await _starWarsService.GetFilmsAsync().ConfigureAwait(true);

            Films.Clear();
            foreach (var film in films)
            {
                Films.Add(film);
            }
        });

        [RelayCommand]
        private Task OnSelectFilm(Film film) => _navigationService.NavigateAsync(PageKey.FilmDetail, film);
    }
}
