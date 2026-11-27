using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Window;
using __NAMESPACE__.Android.Data;
using __NAMESPACE__.Android.Navigation;

namespace __NAMESPACE__.Android;

/// <summary>The frame of the __PROJECT__ Android app: the host activity, the navigation it owns, and the shared window behavior.</summary>
public static class ApplicationHost
{
    /// <summary>
    ///     The base of the host activity. It owns the one <see cref="NavigationController"/>, receives app links,
    ///     delivers the system back gesture to the controller, loads the check-in store once, and saves and
    ///     restores the navigation state. Screens are separate activities that raise events to the controller.
    /// </summary>
    public abstract class Base : Activity
    {
        /// <summary>Gets the navigation controller this host owns.</summary>
        protected NavigationController Controller { get; private set; } = null!;

        /// <summary>Gets the typed check-in store this host loads at startup.</summary>
        protected FileCheckInStore Store { get; private set; } = null!;

        /// <summary>The back callback registered with the activity dispatcher, released with the host.</summary>
        private IOnBackInvokedCallback? backCallback;

        /// <summary>Runs when the host starts: it draws edge to edge, owns the controller, restores navigation state, receives the launching link, and loads the store before showing the first screen.</summary>
        /// <param name="savedInstanceState">The saved navigation state, or null on a fresh start.</param>
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // The target platform draws edge to edge by default, so no window call is needed: the
            // frame background keeps full bleed while the insets listener below keeps the content clear.
            var client = new CheckInApiClient(CheckInApiClient.EmulatorBaseAddress);
            Store = new FileCheckInStore(this, client);
            Controller = new NavigationController(Store);
            NavigationController.Current = Controller;
            Controller.Restore(savedInstanceState);

            var frame = new FrameLayout(this);
            frame.SetOnApplyWindowInsetsListener(new InsetsListener());
            SetContentView(frame);

            Controller.ShowLoading(this);

            if (Intent?.Data is { } firstLink && firstLink.ToString() is { Length: > 0 } firstTarget)
            {
                Controller.ResolveLink(firstTarget);
            }

            LoadAsync();
        }

        /// <summary>Receives an app link that arrives while the host runs, resolving it to one screen with one history behind it.</summary>
        /// <param name="intent">The intent carrying the link.</param>
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            if (intent?.Data is { } link && link.ToString() is { Length: > 0 } target)
            {
                Controller.ResolveLink(target);
                Controller.OpenStart(this);
            }
        }

        /// <summary>Saves the navigation controller state in the activity bundle, so a killed process returns to the same screen.</summary>
        /// <param name="outState">The bundle receiving the navigation state.</param>
        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            ArgumentNullException.ThrowIfNull(outState);
            Controller.Save(outState);
        }

        /// <summary>Delivers the system back gesture to the navigation controller through the activity dispatcher.</summary>
        /// <param name="savedInstanceState">The saved navigation state, or null on a fresh start.</param>
        protected override void OnPostCreate(Bundle? savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            backCallback = new BackCallback(this);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, backCallback);
        }

        /// <summary>Releases the back callback with the host.</summary>
        protected override void OnDestroy()
        {
            if (backCallback is not null)
            {
                OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(backCallback);
                backCallback = null;
            }

            base.OnDestroy();
        }

        /// <summary>Loads the store once, hides the loading overlay, shows the start screen the pending link names, and leaves the host.</summary>
        private async void LoadAsync()
        {
            using var source = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await Store.LoadAsync(source.Token).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                Controller.HideLoading();
                Controller.OpenStart(this);
                Finish();
            });
        }

        /// <summary>Applies the system bar and display cutout insets it reads from the window to the frame, keeping controls and text clear of the bars and cutouts.</summary>
        private sealed class InsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
        {
            /// <summary>Applies the insets to the frame padding while the background keeps full bleed.</summary>
            /// <param name="view">The frame receiving the insets.</param>
            /// <param name="insets">The window insets.</param>
            /// <returns>The insets, unconsumed.</returns>
            public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
            {
                var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
                return insets;
            }
        }

        /// <summary>Delivers the system back gesture to the navigation controller.</summary>
        /// <param name="host">The host activity receiving the gesture.</param>
        private sealed class BackCallback(Activity host) : Java.Lang.Object, IOnBackInvokedCallback
        {
            /// <summary>Runs the back gesture through the controller, leaving only from the first screen.</summary>
            public void OnBackInvoked()
            {
                var controller = NavigationController.Current;

                if (controller is null || !controller.GoBack(host))
                {
                    host.Finish();
                }
            }
        }
    }
}
