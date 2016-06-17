using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml.Documents;

namespace WordCloud.Speech
{
    public delegate void RecognizerStatusChanged(Recognizer sender, SpeechRecognizerStateChangedEventArgs args);
    public delegate void HypothesisGenerated(Recognizer sender, string hypothesis);
    public delegate void ResultGenerated(Recognizer sender, string result);
    public class Recognizer
    {
        public RecognizerStatusChanged RecognizerStatusChanged;
        public HypothesisGenerated HypothesisGenerated;
        public ResultGenerated ResultGenerated;


        private SpeechRecognizer speechRecognizer;    
        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        public async void Start()
        {
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
                await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);
                StartDictating();
            }
            else
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("Permission to access capture resources was not given by the user, reset the application setting in Settings->Privacy->Microphone.");
                await messageDialog.ShowAsync();
            }
        }

        public void Stop()
        {
            StopDictating();
        }

        /// <summary>
        /// Initialize Speech Recognizer and compile constraints.
        /// </summary>
        /// <param name="recognizerLanguage">Language to use for the speech recognizer</param>
        /// <returns>Awaitable task.</returns>
        private async Task InitializeRecognizer(Language recognizerLanguage)
        {
            if (speechRecognizer != null)
            {
                // cleanup prior to re-initializing this scenario.
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

            // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
            // of an audio indicator to help the user understand whether they're being heard.
            speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("Grammar Compilation Failed: " + result.Status.ToString());
                await messageDialog.ShowAsync();
            }
        
            // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
            // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
            // allows us to provide incremental feedback based on what the user's currently saying.
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
        }

        /// <summary>
        /// Begin recognition, or finish the recognition session. 
        /// </summary>
        /// <param name="sender">The button that generated this event</param>
        /// <param name="e">Unused event details</param>
        public async void StartDictating()
        {
            // The recognizer can only start listening in a continuous fashion if the recognizer is currently idle.
            // This prevents an exception from occurring.
            if (speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                try
                {
                    await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                }
                catch (Exception ex)
                {
                    if ((uint)ex.HResult == HResultPrivacyStatementDeclined)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Please enable privace settings", "Exception");
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }
        public async void StopDictating()
        {
            if (speechRecognizer.State != SpeechRecognizerState.Idle)
            {
                // Cancelling recognition prevents any currently recognized speech from
                // generating a ResultGenerated event. StopAsync() will allow the final session to 
                // complete.
                try
                {
                    await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                }
                catch (Exception exception)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                    await messageDialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Provide feedback to the user based on whether the recognizer is receiving their voice input.
        /// </summary>
        /// <param name="sender">The recognizer that is currently running.</param>
        /// <param name="args">The current state of the recognizer.</param>
        private void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            RecognizerStatusChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
               StartDictating();
            }
            else
            {
                Debug.WriteLine("** Recognizer Completed WITH FAIL " + args.Status);
            }
        }

            /// <summary>
            /// Handle events fired when a result is generated. Check for high to medium confidence, and then append the
            /// string to the end of the stringbuffer, and replace the content of the textbox with the string buffer, to
            /// remove any hypothesis text that may be present.
            /// </summary>
            /// <param name="sender">The Recognition session that generated this result</param>
            /// <param name="args">Details about the recognized speech</param>
        private void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High ||
                args.Result.Confidence == SpeechRecognitionConfidence.Low)
            {
                string result = args.Result.Text;
                ResultGenerated?.Invoke(this, result);
            }
            else
            {
                Debug.WriteLine("** Discarded due to rejected Confidence " + args.Result.Text);
            }
        }

        /// <summary>
        /// While the user is speaking, update the textbox with the partial sentence of what's being said for user feedback.
        /// </summary>
        /// <param name="sender">The recognizer that has generated the hypothesis</param>
        /// <param name="args">The hypothesis formed</param>
        private void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            string hypothesis = args.Hypothesis.Text;
            HypothesisGenerated?.Invoke(this,hypothesis);
        }

        /// <summary>
        /// Open the Speech, Inking and Typing page under Settings -> Privacy, enabling a user to accept the 
        /// Microsoft Privacy Policy, and enable personalization.
        private async void openPrivacySettings()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-speechtyping"));
        }
    }
}
