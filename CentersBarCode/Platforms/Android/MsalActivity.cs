using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace CentersBarCode.Platforms.Android;

// Add this activity to handle redirect URL for Google authentication
[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "613797922873-meolimpa6po0vcc8aql1r8asd6sq7n7n.apps.googleusercontent.com")]
public class MsalActivity : BrowserTabActivity
{
}