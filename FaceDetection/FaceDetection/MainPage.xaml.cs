using System;
using Windows.UI.Xaml.Controls;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;

using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceDetection
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //get the keys from https://www.microsoft.com/cognitive-services/ 
        private string faceAPIKey = "6171d608d75f4e47b0a836d16898c1f7";
        private string emotionAPIKey = "df245a9e77ac4a129b9f21080aa1ee43";

        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();

            // Do not cache the state of the UI when suspending/navigating
            NavigationCacheMode = NavigationCacheMode.Disabled;

            // Useful to know when to initialize/clean up the camera
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupAll();
                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                await SetupAll();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await SetupAll();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await CleanupAll();
        }

        #endregion Constructor, lifecycle and navigation


        /// <summary>
        /// Attempts to lock the page orientation, hide the StatusBar (on Phone) and registers event handlers for hardware buttons and orientation sensors
        /// </summary>
        /// <returns></returns>
        private async Task SetupAll()
        {
            // Attempt to lock page to landscape orientation to prevent the CaptureElement from rotating, as this gives a better experience
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            await FaceDetectorControl.Initialize(faceAPIKey,emotionAPIKey);
        }

        /// <summary>
        /// Unregisters event handlers for hardware buttons and orientation sensors, allows the StatusBar (on Phone) to show, and removes the page orientation lock
        /// </summary>
        /// <returns></returns>
        private async Task CleanupAll()
        {
            await FaceDetectorControl.DeInit();

            // Revert orientation preferences
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
        }

    }
}
