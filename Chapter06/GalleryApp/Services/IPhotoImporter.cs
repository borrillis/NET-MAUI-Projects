namespace GalleryApp.Services;

using System.Collections.ObjectModel;
using GalleryApp.Models;

public interface IPhotoImporter
{
    Task<ObservableCollection<Photo>> Get(int start, int count, Quality quality = Quality.Low); 
    Task<ObservableCollection<Photo>> Get(IList<string> filenames, Quality quality = Quality.Low);
}
