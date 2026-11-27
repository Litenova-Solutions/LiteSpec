using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using __NAMESPACE__.Android.Screens.Health;
using __NAMESPACE__.Application.Health;

namespace __NAMESPACE__.Android.Navigation;

/// <summary>The one navigation controller of the __PROJECT__ Android app, owned by the host activity. Screens raise events to it instead of navigating on their own; it shows screens, opens overlays, steps back, resolves app links, and saves and restores its state.</summary>
public sealed class NavigationController
{
    /// <summary>The extra carrying the check-in identifier into the detail screen.</summary>
    public const string ExtraCheckInId = "check_in_id";

    /// <summary>The screen key of the check-in list.</summary>
    private const string ListKey = "list";

    /// <summary>The prefix of a detail screen key, followed by the check-in identifier.</summary>
    private const string DetailPrefix = "detail/";

    /// <summary>Gets or sets the controller the host activity owns. Screens raise events to it.</summary>
    public static NavigationController? Current { get; set; }

    /// <summary>The typed check-in store the host loads at startup.</summary>
    private readonly ICheckInStore store;

    /// <summary>The screen keys behind the current screen, oldest first when enumerated.</summary>
    private readonly Stack<string> history = new();

    /// <summary>The current screen key, or null before the first screen shows.</summary>
    private string? current;

    /// <summary>The open overlay, or null when no overlay is open.</summary>
    private Dialog? overlay;

    /// <summary>The screen key the next start opens, from an app link or a restored state, or null for the list.</summary>
    private string? pending;

    /// <summary>Initializes a new instance of the <see cref="NavigationController"/> class.</summary>
    /// <param name="store">The typed check-in store the host loads at startup.</param>
    public NavigationController(ICheckInStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    /// <summary>Gets the typed check-in store the host loads at startup.</summary>
    public ICheckInStore Store => store;

    /// <summary>The screen key of the detail of one check-in.</summary>
    /// <param name="checkInId">The check-in identifier the detail shows.</param>
    /// <returns>The detail screen key for the check-in.</returns>
    public static string DetailKey(string checkInId)
    {
        ArgumentException.ThrowIfNullOrEmpty(checkInId);
        return DetailPrefix + checkInId;
    }

    /// <summary>Shows the start screen the pending link or restored state names, or the list, and leaves the caller behind.</summary>
    /// <param name="caller">The activity starting the screen.</param>
    public void OpenStart(Activity caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        var key = pending ?? current ?? ListKey;
        pending = null;
        current = null;
        Show(caller, key);
    }

    /// <summary>Shows the check-in list, recording the current screen behind it.</summary>
    /// <param name="caller">The activity raising the event.</param>
    public void OpenList(Activity caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        Show(caller, ListKey);
    }

    /// <summary>Shows the detail of one check-in, recording the current screen behind it.</summary>
    /// <param name="caller">The activity raising the event.</param>
    /// <param name="checkInId">The check-in identifier the detail shows.</param>
    public void OpenDetail(Activity caller, string checkInId)
    {
        ArgumentNullException.ThrowIfNull(caller);
        Show(caller, DetailKey(checkInId));
    }

    /// <summary>Steps back: it closes the open overlay first, then steps back one screen, and reports whether it handled the gesture. The app leaves only from the first screen.</summary>
    /// <param name="screen">The activity receiving the gesture.</param>
    /// <returns>True when the controller handled the gesture; otherwise false, so the screen leaves.</returns>
    public bool GoBack(Activity screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (overlay is not null)
        {
            overlay.Dismiss();
            overlay = null;
            return true;
        }

        if (history.Count == 0)
        {
            return false;
        }

        var previous = history.Pop();
        current = previous;
        screen.StartActivity(IntentFor(screen, previous));
        screen.Finish();
        return true;
    }

    /// <summary>Resolves an app link to one screen key the next start opens, with one history behind it.</summary>
    /// <param name="target">The app link target.</param>
    public void ResolveLink(string target)
    {
        ArgumentNullException.ThrowIfNull(target);
        pending = ScreenKey(target);
    }

    /// <summary>Opens the loading overlay above the current screen.</summary>
    /// <param name="host">The activity showing the overlay.</param>
    public void ShowLoading(Activity host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (overlay is not null)
        {
            return;
        }

        var dialog = new Dialog(host);
        var bar = new ProgressBar(host);
        dialog.SetContentView(bar);
        dialog.SetCancelable(false);
        dialog.Show();
        overlay = dialog;
    }

    /// <summary>Hides the loading overlay when the store turns ready.</summary>
    public void HideLoading()
    {
        overlay?.Dismiss();
        overlay = null;
    }

    /// <summary>Saves the navigation state in the activity bundle.</summary>
    /// <param name="state">The bundle receiving the current screen and the history.</param>
    public void Save(Bundle state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.PutStringArray("history", history.Reverse().ToArray());
        state.PutString("current", current);
    }

    /// <summary>Restores the navigation state from the activity bundle.</summary>
    /// <param name="state">The bundle holding the saved state, or null on a fresh start.</param>
    public void Restore(Bundle? state)
    {
        history.Clear();
        current = null;
        pending = null;

        if (state is null)
        {
            return;
        }

        var saved = state.GetStringArray("history");

        if (saved is not null)
        {
            foreach (var key in saved)
            {
                if (key is not null)
                {
                    history.Push(key);
                }
            }
        }

        current = state.GetString("current");
        pending = current;
        current = null;
    }

    /// <summary>Shows one screen, recording the current screen behind it, and leaves the caller behind.</summary>
    /// <param name="caller">The activity raising the event.</param>
    /// <param name="key">The screen key to show.</param>
    private void Show(Activity caller, string key)
    {
        if (current is not null)
        {
            history.Push(current);
        }

        current = key;
        caller.StartActivity(IntentFor(caller, key));
        caller.Finish();
    }

    /// <summary>The intent showing one screen key.</summary>
    /// <param name="context">The context starting the screen.</param>
    /// <param name="key">The screen key to show.</param>
    /// <returns>The intent for the screen.</returns>
    private static Intent IntentFor(Context context, string key)
    {
        if (key.StartsWith(DetailPrefix, StringComparison.Ordinal) && key.Length > DetailPrefix.Length)
        {
            var detail = new Intent(context, typeof(HealthDetailActivity));
            detail.PutExtra(ExtraCheckInId, key[DetailPrefix.Length..]);
            return detail;
        }

        return new Intent(context, typeof(HealthListActivity));
    }

    /// <summary>The screen key one app link names: the detail for a check-in link, else the list.</summary>
    /// <param name="target">The app link target.</param>
    /// <returns>The screen key, or null for the list.</returns>
    private static string? ScreenKey(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var link))
        {
            return null;
        }

        var path = link.AbsolutePath.Trim('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 2 && string.Equals(segments[0], "check-ins", StringComparison.OrdinalIgnoreCase))
        {
            return DetailKey(segments[1]);
        }

        return null;
    }
}
