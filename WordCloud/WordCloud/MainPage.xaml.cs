
using Gma.CodeCloud.Controls;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist.En;
using Gma.CodeCloud.Controls.TextAnalyses.Extractors;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Gma.CodeCloud.Controls.TextAnalyses.Stemmers;
using Gma.CodeCloud.Controls.TextAnalyses.Stemmers.En;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Media.SpeechRecognition;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WordCloud.Speech;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WordCloud
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        String inputText = "";//"Lorem ipsum dolor sit amet Lorem ipsum dolor sit amet";//, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

        private IBlacklist _blacklist;
        private IProgressIndicator _progress;
        private Recognizer _recognizer;

        public MainPage()
        {
            this.InitializeComponent();
            _blacklist = new BannedWords();// CommonWords();
            _progress = new ProgressBarWrapper(ProgressBar);

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
                UpdateWords();
                _recognizer.RecognizerStatusChanged += Recognizer_RecognizerStatusChanged;
                _recognizer.HypothesisGenerated += Recognizer_HypothesisGenerated;
                _recognizer.ResultGenerated += Recognizer_ResultGenerated;
                _recognizer.Start();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateWords();
            _recognizer.RecognizerStatusChanged += Recognizer_RecognizerStatusChanged;
            _recognizer.HypothesisGenerated += Recognizer_HypothesisGenerated;
            _recognizer.ResultGenerated += Recognizer_ResultGenerated;
            _recognizer.Start();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _recognizer.RecognizerStatusChanged -= Recognizer_RecognizerStatusChanged;
            _recognizer.HypothesisGenerated -= Recognizer_HypothesisGenerated;
            _recognizer.ResultGenerated -= Recognizer_ResultGenerated;
            _recognizer.Stop();
        }

        private void UpdateWords()
        {
            if(inputText.Length < 3)
            {
                return;
            }

            IEnumerable<string> terms = new StringExtractor(inputText, _progress);

            CloudControl.WeightedWords =
                terms
                    .Filter(_blacklist)
                    .CountOccurences()
                    .SortByOccurences();   
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
        public async void Recognizer_ResultGenerated(Recognizer sender, string result)
        {
            Debug.WriteLine("ResultGenerated " + result);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                resultBox.Foreground = new SolidColorBrush(Colors.Black);
                resultBox.Text = result;

                inputText = inputText + " " + result;
                UpdateWords();
            });
        }

        internal class ProgressBarWrapper : IProgressIndicator
        {
            private readonly ProgressBar m_ProgressBar;

            public ProgressBarWrapper(ProgressBar toolStripProgressBar)
            {
                m_ProgressBar = toolStripProgressBar;
            }

            public Double Value
            {
                get { return m_ProgressBar.Value; }
                set { m_ProgressBar.Value = value; }
            }

            public virtual Double Maximum
            {
                get { return m_ProgressBar.Maximum; }
                set { m_ProgressBar.Maximum = value; }
            }

            public virtual void Increment(int value)
            {
               // m_ProgressBar.Increment(value);
               // Application.DoEvents();
            }
        }

        internal class BannedWords : CommonBlacklist
        {
            private static readonly string[] s_TopCommonWords =
                new[]
                {"I"};

            public BannedWords(): base(s_TopCommonWords)
            {
            }
        }
    }
}
