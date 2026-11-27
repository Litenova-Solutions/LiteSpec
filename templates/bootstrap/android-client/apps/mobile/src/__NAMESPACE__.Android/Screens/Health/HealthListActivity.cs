using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Window;
using __NAMESPACE__.Android.Data;
using __NAMESPACE__.Android.Navigation;

namespace __NAMESPACE__.Android.Screens.Health;

/// <implements>screen/__SCOPE__-app.check-in-list</implements>
[Activity(Label = "@string/check_in_list_title", Theme = "@style/AppTheme", Exported = false)]
public sealed class HealthListActivity : Activity
{
    /// <summary>The back callback delivering the system back gesture to the navigation controller.</summary>
    private IOnBackInvokedCallback? backCallback;

    /// <summary>The load cancelled with the screen.</summary>
    private CancellationTokenSource? load;

    /// <summary>The row identifiers the list shows, in row order.</summary>
    private readonly List<string> ids = new();

    /// <summary>The adapter showing the rows.</summary>
    private ArrayAdapter<string>? rows;

    /// <summary>The status text shown for the empty and failed states.</summary>
    private TextView? status;

    /// <summary>The retry button shown for the failed state.</summary>
    private Button? retry;

    /// <summary>Runs when the list starts: it draws edge to edge, reads the typed store the host loaded, and shows the rows, the empty state, or the failed state.</summary>
    /// <param name="savedInstanceState">The saved state, or null on a fresh start.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var resources = Resources;
        ArgumentNullException.ThrowIfNull(resources);
        var pad = resources.GetDimensionPixelSize(Resource.Dimension.list_row_padding);

        var frame = new LinearLayout(this) { Orientation = Orientation.Vertical };
        frame.SetPadding(pad, 0, pad, 0);
        frame.SetOnApplyWindowInsetsListener(new InsetsListener(pad));

        var heading = new TextView(this);
        heading.SetText(Resource.String.check_in_list_title);
        heading.SetTextAppearance(Resource.Style.CheckInTitle);
        frame.AddView(heading);

        var list = new ListView(this);
        rows = new RowAdapter(this, pad, resources.GetDimensionPixelSize(Resource.Dimension.touch_target), ids);
        list.Adapter = rows;
        list.ItemClick += OnRowOpened;
        frame.AddView(list);

        status = new TextView(this);
        status.SetTextAppearance(Resource.Style.CheckInMuted);
        status.Visibility = ViewStates.Gone;
        frame.AddView(status);

        retry = new Button(this);
        retry.SetText(Resource.String.check_in_list_retry);
        retry.ContentDescription = resources.GetString(Resource.String.check_in_list_retry);
        retry.SetMinimumHeight(resources.GetDimensionPixelSize(Resource.Dimension.touch_target));
        retry.Visibility = ViewStates.Gone;
        retry.Click += OnRetry;
        frame.AddView(retry);

        SetContentView(frame);
        Refresh();
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

    /// <summary>Raises the open event to the navigation controller for the tapped row.</summary>
    /// <param name="sender">The list raising the event.</param>
    /// <param name="args">The tapped row position.</param>
    private void OnRowOpened(object? sender, AdapterView.ItemClickEventArgs args)
    {
        if (args.Position >= 0 && args.Position < ids.Count)
        {
            NavigationController.Current?.OpenDetail(this, ids[args.Position]);
        }
    }

    /// <summary>Reloads the store when the visitor retries a failed list.</summary>
    /// <param name="sender">The retry button.</param>
    /// <param name="args">The tap.</param>
    private void OnRetry(object? sender, EventArgs args)
    {
        Refresh();
    }

    /// <summary>Refreshes the rows from the typed store, showing the empty or failed state with no rows to show.</summary>
    private void Refresh()
    {
        var controller = NavigationController.Current;

        if (controller is null)
        {
            Finish();
            return;
        }

        load?.Cancel();
        load?.Dispose();
        load = new CancellationTokenSource();
        ReloadAsync(controller, load.Token);
    }

    /// <summary>Reloads the store and draws the rows, the empty state, or the failed state.</summary>
    /// <param name="controller">The navigation controller owning the store.</param>
    /// <param name="cancellationToken">The token cancelling the load.</param>
    private async void ReloadAsync(NavigationController controller, CancellationToken cancellationToken)
    {
        try
        {
            var probe = new CheckInApiClient(CheckInApiClient.EmulatorBaseAddress);
            _ = await probe.ListAsync(cancellationToken).ConfigureAwait(false);
            await controller.Store.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        RunOnUiThread(() =>
        {
            var store = controller.Store;
            ids.Clear();

            foreach (var checkIn in store.Current)
            {
                ids.Add(checkIn.Id);
            }

            rows?.NotifyDataSetChanged();

            var hasRows = ids.Count > 0;
            var failed = !hasRows;

            if (status is not null)
            {
                status.SetText(failed ? Resource.String.check_in_list_failed : Resource.String.check_in_list_empty);
                status.ContentDescription = failed
                    ? Resources?.GetString(Resource.String.check_in_list_failed)
                    : Resources?.GetString(Resource.String.check_in_list_empty);
                status.Visibility = hasRows ? ViewStates.Gone : ViewStates.Visible;
            }

            if (retry is not null)
            {
                retry.Visibility = failed ? ViewStates.Visible : ViewStates.Gone;
            }
        });
    }

    /// <summary>Applies the system bar and display cutout insets it reads from the window to the list, keeping rows clear of the bars and cutouts.</summary>
    /// <param name="top">The top padding above the insets.</param>
    private sealed class InsetsListener(int top) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        /// <summary>Applies the insets below the title padding.</summary>
        /// <param name="view">The list frame receiving the insets.</param>
        /// <param name="insets">The window insets.</param>
        /// <returns>The insets, unconsumed.</returns>
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
            view.SetPadding(view.PaddingLeft, top + bars.Top, view.PaddingRight, bars.Bottom);
            return insets;
        }
    }

    /// <summary>Draws one check-in row with the check-in title style and a full touch target.</summary>
    private sealed class RowAdapter : ArrayAdapter<string>
    {
        /// <summary>The row padding from resources.</summary>
        private readonly int rowPadding;

        /// <summary>The touch target from resources.</summary>
        private readonly int touchTarget;

        /// <summary>Initializes a new instance of the <see cref="RowAdapter"/> class.</summary>
        /// <param name="context">The screen showing the rows.</param>
        /// <param name="rowPadding">The row padding from resources.</param>
        /// <param name="touchTarget">The touch target from resources.</param>
        /// <param name="rows">The row identifiers.</param>
        internal RowAdapter(Context context, int rowPadding, int touchTarget, List<string> rows)
            : base(context, global::Android.Resource.Layout.SimpleListItem1, rows)
        {
            this.rowPadding = rowPadding;
            this.touchTarget = touchTarget;
        }

        /// <summary>Draws one row with the resource style, padding, touch target, and content description.</summary>
        /// <param name="position">The row position.</param>
        /// <param name="convertView">The recycled row, or null.</param>
        /// <param name="parent">The list holding the row.</param>
        /// <returns>The row view.</returns>
        public override View GetView(int position, View? convertView, ViewGroup parent)
        {
            var row = (base.GetView(position, convertView, parent) as TextView) ?? new TextView(Context);
            row.SetTextAppearance(Resource.Style.CheckInBody);
            row.SetPadding(rowPadding, rowPadding, rowPadding, rowPadding);
            row.SetMinimumHeight(touchTarget);
            row.ContentDescription = row.Text;
            return row;
        }
    }

    /// <summary>Delivers the system back gesture to the navigation controller.</summary>
    /// <param name="screen">The list screen receiving the gesture.</param>
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
