using DrawboardCodingExercise.ViewModel;

using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DrawboardCodingExercise.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Films : Page
    {
        public Films()
        {
            this.InitializeComponent();
        }

        private void FilmsList_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (DataContext is FilmsViewModel viewModel && viewModel.SelectFilmCommand.CanExecute(e.ClickedItem))
            {
                viewModel.SelectFilmCommand.Execute(e.ClickedItem);
            }
        }
    }
}
