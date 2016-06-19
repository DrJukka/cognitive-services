using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WordCloud.Speech;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ShowMyAddress
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Recognizer _recognizer;

        public MainPage()
        {
            this.InitializeComponent();

            myMap.Center = new Geopoint(new BasicGeoposition()
            {   //Geopoint for Bristol
                Latitude = 2.5833,
                Longitude = 51.4500
            });
            myMap.ZoomLevel = 11;

            _recognizer = new Recognizer();
        }

        private void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                _recognizer.RecognizerStatusChanged -= Recognizer_RecognizerStatusChanged;
                _recognizer.HypothesisGenerated -= Recognizer_HypothesisGenerated;
                _recognizer.ResultGenerated -= Recognizer_ResultGenerated;
                _recognizer.Stop();
            }
        }

        private void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                _recognizer.RecognizerStatusChanged += Recognizer_RecognizerStatusChanged;
                _recognizer.HypothesisGenerated += Recognizer_HypothesisGenerated;
                _recognizer.ResultGenerated += Recognizer_ResultGenerated;
                _recognizer.Start(SpeechRecognitionScenario.FormFilling);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _recognizer.RecognizerStatusChanged += Recognizer_RecognizerStatusChanged;
            _recognizer.HypothesisGenerated += Recognizer_HypothesisGenerated;
            _recognizer.ResultGenerated += Recognizer_ResultGenerated;
            _recognizer.Start(SpeechRecognitionScenario.FormFilling);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _recognizer.RecognizerStatusChanged -= Recognizer_RecognizerStatusChanged;
            _recognizer.HypothesisGenerated -= Recognizer_HypothesisGenerated;
            _recognizer.ResultGenerated -= Recognizer_ResultGenerated;
            _recognizer.Stop();
        }

        private async Task ReStartRecognition()
        {
            await _recognizer.Stop();
            _recognizer.Start(SpeechRecognitionScenario.Dictation);
        }

        public async void Recognizer_RecognizerStatusChanged(Recognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            string state = args.State.ToString();
            Debug.WriteLine("RecognizerStatusChanged " + state);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusBox.Text = state;
            });
        }
        public async void Recognizer_HypothesisGenerated(Recognizer sender, string hypothesis)
        {
            Debug.WriteLine("HypothesisGenerated " + hypothesis);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                resultBox.Foreground = new SolidColorBrush(Colors.Gray);
                resultBox.Text = hypothesis + "...";
            });
        }
        public async void Recognizer_ResultGenerated(Recognizer sender, string result, SpeechRecognitionConfidence confidence)
        {
            Debug.WriteLine("ResultGenerated " + result);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (confidence == SpeechRecognitionConfidence.Medium
                || confidence == SpeechRecognitionConfidence.High)
                //|| confidence == SpeechRecognitionConfidence.Low)
                {
                    resultBox.Foreground = new SolidColorBrush(Colors.Black);
                    resultBox.Text = result;

                    Geopoint newPointn = await myMap.FindLocation(result);
                    if (newPointn != null)
                    {
                        myMap.Center = newPointn;
                    }
                }
                else
                {
                    Debug.WriteLine("SpeechRecognitionConfidence " + confidence);
                }


                lastResultBox.Text = resultBox.Text;
                resultBox.Text = "";
            });

            ReStartRecognition();

        }
    }
}
