using System.Windows;

namespace Creaous.LenovoDriverManager;

public class Category : DependencyObject
{
    public string Name { get; set; }
    public List<Update> Updates { get; set; }
}

public class Update : DependencyObject
{
    public string Name { get; set; }
    public string URL { get; set; }
    public string MD5 { get; set; }
    public string Size { get; set; }
    public string Version { get; set; }
}