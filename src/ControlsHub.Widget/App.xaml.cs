using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ControlsHub.Widget
{
    /// <summary>
    /// Widget app. Activated by Game Bar (protocol ms-gamebarwidget, XboxGameBarWidgetActivatedEventArgs)
    /// or launched normally from Start (same page in a normal window, handy for testing).
    /// Also hosts the in process AppService "ControlsHub.Broker" the broker connects to.
    /// </summary>
    sealed partial class App : Application
    {
        private XboxGameBarWidget _widget;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            XboxGameBarWidgetActivatedEventArgs widgetArgs = null;
            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = args as IProtocolActivatedEventArgs;
                if (protocolArgs != null && protocolArgs.Uri.Scheme.Equals("ms-gamebarwidget"))
                {
                    widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                }
            }
            if (widgetArgs == null) return;

            // IsLaunchActivation: Game Bar created a new widget instance with a new CoreWindow.
            // Later activations reuse the instance we hold.
            if (widgetArgs.IsLaunchActivation)
            {
                var rootFrame = new Frame();
                Window.Current.Content = rootFrame;
                _widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
                rootFrame.Navigate(typeof(WidgetPage), _widget);
                Window.Current.Closed += WidgetWindow_Closed;
                Window.Current.Activate();
                BrokerConnection.EnsureBroker();
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }
            if (!e.PrelaunchActivated)
            {
                if (rootFrame.Content == null) rootFrame.Navigate(typeof(WidgetPage), null);
                Window.Current.Activate();
                BrokerConnection.EnsureBroker();
            }
        }

        /// <summary>The broker opened the AppService: keep the connection until it closes.</summary>
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            var details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (details == null || details.Name != BrokerConnection.ServiceName) return;
            if (details.CallerPackageFamilyName != Package.Current.Id.FamilyName)
            {
                // Only our own broker may talk to the widget.
                args.TaskInstance.GetDeferral().Complete();
                return;
            }
            BrokerConnection.Attach(details.AppServiceConnection, args.TaskInstance);
        }

        private void WidgetWindow_Closed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
        {
            _widget = null;
            Window.Current.Closed -= WidgetWindow_Closed;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            _widget = null;
            deferral.Complete();
        }
    }
}
