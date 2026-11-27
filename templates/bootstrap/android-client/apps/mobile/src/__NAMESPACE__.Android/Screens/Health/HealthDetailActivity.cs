using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Window;
using __NAMESPACE__.Android.Data;
using __NAMESPACE__.Android.Navigation;

namespace __NAMESPACE__.Android.Screens.Health;

/// <implements>screen/__SCOPE__-app.check-in-detail</implements>
[Activity(Label = "@string/check_in_detail_title", Theme = "@style/AppTheme", Exported = false)]
public sealed class HealthDetailActivity : Activity
{
    /// <summary>The back callback delivering the system back gesture to the navigation controller.</summary>
    private IOnBackInvokedCallback? backCallback;

    /// <summary>The load cancelled with the screen.</summary>
    private CancellationTokenSource? load;

    /// <summary>The check-in identifier the detail shows.</summary>
    private string? checkInId;

    /// <summary>The identifier row.</summary>
    private TextView? identifier;

    /// <summary>The state row.</summary>
    private TextView? state;

    /// <summary>The status text shown for the failed state.</summary>
    private TextView? status;

    /// <summary>Runs when the detail starts: it draws edge to edge, restores the check-in identifier, and reads the check-in over HTTP.</summary>
    /// <param name="savedInstanceState">The saved check-in identifier, or null on a fresh start.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        checkInId = savedInstanceState?.GetString(NavigationController.ExtraCheckInId)
            ?? Intent?.GetStringExtra(NavigationController.ExtraCheckInId);

        var resources = Resources;
        ArgumentNullException.ThrowIfNull(resources);
        var pad = resources.GetDimensionPixelSize(Resource.Dimension.list_row_padding);

        var frame = new LinearLayout(this) { Orientation = Orientation.Vertical };
        frame.SetPadding(pad, 0, pad, 0);
        frame.SetOnApplyWindowInsetsListener(new InsetsListener(pad));

        var heading = new TextView(this);
        heading.SetText(Resource.String.check_in_detail_title);
        heading.SetTextAppearance(Resource.Style.CheckInTitle);
        frame.AddView(heading);

        var identifierLabel = new TextView(this);
        identifierLabel.SetText(Resource.String.check_in_detail_identifier);
        identifierLabel.SetTextAppearance(Resource.Style.CheckInMuted);
        frame.AddView(identifierLabel);

        identifier = new TextView(this);
        identifier.SetTextAppearance(Resource.Style.CheckInBody);
        frame.AddView(identifier);

        var stateLabel = new TextView(this);
        stateLabel.SetText(Resource.String.check_in_detail_state);
        stateLabel.SetTextAppearance(Resource.Style.CheckInMuted);
        frame.AddView(stateLabel);

        state = new TextView(this);
        state.SetTextAppearance(Resource.Style.CheckInBody);
        frame.AddView(state);

        status = new TextView(this);
        status.SetTextAppearance(Resource.Style.CheckInMuted);
        status.Visibility = ViewStates.Gone;
        frame.AddView(status);

        SetContentView(frame);

        if (checkInId is { Length: > 0 })
        {
            ReadAsync();
        }
        else if (status is not null)
        {
            status.SetText(Resource.String.check_in_detail_failed);
            status.Visibility = ViewStates.Visible;
        }
    }

    /// <summary>Saves the check-in identifier in the activity bundle.</summary>
    /// <param name="outState">The bundle receiving the identifier.</param>
    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);
        ArgumentNullException.ThrowIfNull(outState);

        if (checkInId is { Length: > 0 } identifier)
        {
            outState.PutString(NavigationController.ExtraCheckInId, identifier);
        }
    }

    /// <summary>Delivers the system back gesture to the navigation controller through the activity dispatcher.</summary>
    /// <param name="savedInstanceState">The saved state, or null on a fresh start.</param>
    protected override void OnPostCreate(Bundle? savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        backCallback = new BackCallback(this);
        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, backCallback);
    }

    /// <summary>Releases the back callback and the load with the screen.</summary>
    protected override void OnDestroy()
    {
        if (backCallback is not null)
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(backCallback);
            backCallback = null;
        }

        load?.Cancel();
        load?.Dispose();
        load = null;

        base.OnDestroy();
    }

    /// <summary>Reads the check-in over HTTP and draws it, or the failed state.</summary>
    private void ReadAsync()
    {
        load?.Cancel();
        load?.Dispose();
        load = new CancellationTokenSource();
        LoadAsync(load.Token);
    }

    /// <summary>Loads one check-in and draws its rows or the failed state.</summary>
    /// <param name="cancellationToken">The token cancelling the read.</param>
    private async void LoadAsync(CancellationToken cancellationToken)
    {
        if (checkInId is not { Length: > 0 } identifier)
        {
            return;
        }

        var client = new CheckInApiClient(CheckInApiClient.EmulatorBaseAddress);
        var result = await client.ReadAsync(identifier, cancellationToken).ConfigureAwait(false);

        RunOnUiThread(() =>
        {
            if (result is CheckInDetailResult.Loaded loaded)
            {
                if (this.identifier is not null)
                {
                    this.identifier.Text = loaded.CheckIn.Id;
                    this.identifier.ContentDescription = loaded.CheckIn.Id;
                }

                if (state is not null)
                {
                    state.SetText(Resource.String.check_in_state_recorded);
                    state.ContentDescription = state.Text;
                }

                if (status is not null)
                {
                    status.Visibility = ViewStates.Gone;
                }
            }
            else if (status is not null)
            {
                status.SetText(Resource.String.check_in_detail_failed);
                status.Visibility = ViewStates.Visible;
            }
        });
    }

    /// <summary>Applies the system bar and display cutout insets it reads from the window to the detail, keeping rows clear of the bars and cutouts.</summary>
    /// <param name="top">The top padding above the insets.</param>
    private sealed class InsetsListener(int top) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        /// <summary>Applies the insets below the title padding.</summary>
        /// <param name="view">The detail frame receiving the insets.</param>
        /// <param name="insets">The window insets.</param>
        /// <returns>The insets, unconsumed.</returns>
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
            view.SetPadding(view.PaddingLeft, top + bars.Top, view.PaddingRight, bars.Bottom);
            return insets;
        }
    }

    /// <summary>Delivers the system back gesture to the navigation controller.</summary>
    /// <param name="screen">The detail screen receiving the gesture.</param>
    private sealed class BackCallback(Activity screen) : Java.Lang.Object, IOnBackInvokedCallback
    {
        /// <summary>Runs the back gesture through the controller, leaving only from the first screen.</summary>
        public void OnBackInvoked()
        {
            var controller = NavigationController.Current;

            if (controller is null || !controller.GoBack(screen))
            {
                screen.Finish();
            }
        }
    }
}
