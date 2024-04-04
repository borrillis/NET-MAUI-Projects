namespace GalleryApp.Models;

public class Photo
{
    public string Filename { get; set; } = string.Empty;
    public byte[] Bytes { get; set; } = [];
}
