using Android.App;
using Android.Content;

namespace __NAMESPACE__.Android;

/// <summary>The host activity of the __PROJECT__ Android app: the launcher owning the one navigation controller.</summary>
[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true, Theme = "@style/AppTheme")]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "https", DataHost = "__SCOPE__.example.com", AutoVerify = true)]
public sealed class MainActivity : ApplicationHost.Base
{
}
