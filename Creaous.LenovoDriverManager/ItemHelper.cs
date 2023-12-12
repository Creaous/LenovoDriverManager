// https://stackoverflow.com/a/28613398

using System.Windows;

namespace Creaous.LenovoDriverManager;

public class ItemHelper : DependencyObject
{
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked",
        typeof(bool?), typeof(ItemHelper), new PropertyMetadata(false, OnIsCheckedPropertyChanged));

    public static readonly DependencyProperty ParentProperty =
        DependencyProperty.RegisterAttached("Parent", typeof(object), typeof(ItemHelper));

    private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Category && ((bool?)e.NewValue).HasValue)
            foreach (var p in (d as Category).Updates)
                SetIsChecked(p, (bool?)e.NewValue);

        if (d is Update)
        {
            var checkedValue = ((d as Update).GetValue(ParentProperty) as Category).Updates
                .Where(x => GetIsChecked(x) == true).Count();
            var uncheckedValue = ((d as Update).GetValue(ParentProperty) as Category).Updates
                .Where(x => GetIsChecked(x) == false).Count();
            if (uncheckedValue > 0 && checkedValue > 0)
            {
                SetIsChecked((d as Update).GetValue(ParentProperty) as DependencyObject, null);
                return;
            }

            if (checkedValue > 0)
            {
                SetIsChecked((d as Update).GetValue(ParentProperty) as DependencyObject, true);
                return;
            }

            SetIsChecked((d as Update).GetValue(ParentProperty) as DependencyObject, false);
        }
    }

    public static void SetIsChecked(DependencyObject element, bool? IsChecked)
    {
        element.SetValue(IsCheckedProperty, IsChecked);
    }

    public static bool? GetIsChecked(DependencyObject element)
    {
        return (bool?)element.GetValue(IsCheckedProperty);
    }

    public static void SetParent(DependencyObject element, object Parent)
    {
        element.SetValue(ParentProperty, Parent);
    }

    public static object GetParent(DependencyObject element)
    {
        return element.GetValue(ParentProperty);
    }
}