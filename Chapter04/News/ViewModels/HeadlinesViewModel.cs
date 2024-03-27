using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using News.Models;
using News.Services;

namespace News.ViewModels;

public partial class HeadlinesViewModel(INewsService newsService, INavigate navigation) : ViewModel(navigation)
{
    private readonly INewsService newsService = newsService;

    [ObservableProperty]
    private NewsResult? currentNews;

    public async Task Initialize(string? scope) => await Initialize(scope?.ToLower() switch
    {
        "local" => NewsScope.Local,
        "global" => NewsScope.Global,
        "headlines" => NewsScope.Headlines,
        _ => NewsScope.Headlines
    });

    public async Task Initialize(NewsScope scope)
    {
        CurrentNews = await newsService.GetNews(scope);
    }

    [RelayCommand]
    public async Task ItemSelected(object selectedItem)
    {
        if (selectedItem is Article selectedArticle)
        {
            var url = HttpUtility.UrlEncode(selectedArticle.Url);
            await Navigation.NavigateTo($"articleview?url={url}");
        }
    }
}
