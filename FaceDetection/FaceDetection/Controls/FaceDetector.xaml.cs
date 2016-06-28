using System;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Windows.Graphics.Imaging;
using System.Collections.Generic;
using Windows.Media.FaceAnalysis;
using Windows.UI;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Emotion;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.ProjectOxford.Emotion.Contract;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FaceDetection.Controls
{
    public sealed partial class FaceDetector : UserControl
    {
        private IFaceServiceClient _faceServiceClient = null;
        private EmotionServiceClient _emotionServiceClient = null;
        public ObservableCollection<FaceDetails> FaceCollection
        {
            get;
            set;
        }

        public ObservableCollection<EmotionDetails> EmotionCollection
        {
            get;
            set;
        }

        // Receive notifications about rotation of the device and UI and apply any necessary rotation to the preview stream and UI controls
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        private FaceDetectionEffect _faceDetectionEffect;

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private bool _isInitialized;

        private IMediaEncodingProperties _previewProperties;

        // Information about the camera device
        private bool _mirroringPreview;
        private bool _externalCamera;

        // Rotation metadata to apply to the preview stream and recorded videos (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        public FaceDetector()
        {
            this.InitializeComponent();
            FaceCollection = new ObservableCollection<FaceDetails>();
            FaceListBox.ItemsSource = FaceCollection;
            EmotionCollection = new ObservableCollection<EmotionDetails>();
            EmotionListBox.ItemsSource = EmotionCollection;
        }

        /// <summary>
        /// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, starts preview and unlocks the UI
        /// </summary>
        /// <returns></returns>
        public async Task Initialize(string faceKey, string emotionKey)
        {
            _faceServiceClient = new FaceServiceClient(faceKey);
            _emotionServiceClient = new EmotionServiceClient(emotionKey);

            Debug.WriteLine("Initialize-Facedetector");

            if (_mediaCapture == null)
            {
                // Attempt to get the front camera if one is available, but use any camera device if not
                //my lice-cam has Microsoft as part of its name, so I can select it with Microsoft name
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Front, "Microsoft");

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _mediaCapture.Failed += MediaCapture_Failed;

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                _displayOrientation = _displayInformation.CurrentOrientation;
                if (_orientationSensor != null)
                {
                    _deviceOrientation = _orientationSensor.GetCurrentOrientation();
                }
                
                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                }

                // If initialization succeeded, start the preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel

                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }

                    await StartPreviewAsync();

                    // Clear any rectangles that may have been left over from a previous instance of the effect
                    FacesCanvas.Children.Clear();
                    await CreateFaceDetectionEffectAsync();
                }
            }
        }

        /// <summary>
        /// Cleans up the camera resources (after stopping any video recording and/or preview if necessary) and unregisters from MediaCapture events
        /// </summary>
        /// <returns></returns>
        public async Task DeInit()
        {
            RegisterEventHandlers();
            Debug.WriteLine("DeInit-Facedetector");
            UnregisterEventHandlers();

            if (_isInitialized)
            {
                if (_faceDetectionEffect != null)
                {
                    await CleanUpFaceDetectionEffectAsync();
                }

                if (_previewProperties != null)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        /// <summary>
        /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on
        /// </summary>
        /// <returns></returns>
        private async Task StartPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start the preview
            await _mediaCapture.StartPreviewAsync();
            _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);

            // Initialize the preview to the current orientation
            if (_previewProperties != null)
            {
                await SetPreviewRotationAsync();
            }
        }

        /// <summary>
        /// Gets the current orientation of the UI in relation to the device (when AutoRotationPreferences cannot be honored) and applies a corrective rotation to the preview
        /// </summary>
        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (_externalCamera) return;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Stops the preview and deactivates a display request, to allow the screen to go into power saving modes
        /// </summary>
        /// <returns></returns>
        private async Task StopPreviewAsync()
        {
            // Stop the preview
            _previewProperties = null;
            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Cleanup the UI
                PreviewControl.Source = null;

                // Allow the device screen to sleep now that the preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        /// <summary>
        /// Adds face detection to the preview stream, registers for its events, enables it, and gets the FaceDetectionEffect instance
        /// </summary>
        /// <returns></returns>
        private async Task CreateFaceDetectionEffectAsync()
        {
            // Create the definition, which will contain some initialization settings
            var definition = new FaceDetectionEffectDefinition();

            // To ensure preview smoothness, do not delay incoming samples
            definition.SynchronousDetectionEnabled = false;

            // In this scenario, choose detection speed over accuracy
            definition.DetectionMode = FaceDetectionMode.HighPerformance;

            // Add the effect to the preview stream
            _faceDetectionEffect = (FaceDetectionEffect)await _mediaCapture.AddVideoEffectAsync(definition, MediaStreamType.VideoPreview);

            // Register for face detection events
            _faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;

            // Choose the shortest interval between detection events
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);

            // Start detecting faces
            _faceDetectionEffect.Enabled = true;
        }

        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            // Ask the UI thread to render the face bounding boxes
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFaces(args.ResultFrame.DetectedFaces));
        }

        /// <summary>
        ///  Disables and removes the face detection effect, and unregisters the event handler for face detection
        /// </summary>
        /// <returns></returns>
        private async Task CleanUpFaceDetectionEffectAsync()
        {
            // Disable detection
            _faceDetectionEffect.Enabled = false;

            // Unregister the event handler
            _faceDetectionEffect.FaceDetected -= FaceDetectionEffect_FaceDetected;

            // Remove the effect from the preview stream
            await _mediaCapture.ClearEffectsAsync(MediaStreamType.VideoPreview);

            // Clear the member variable that held the effect instance
            _faceDetectionEffect = null;
        }

        /// <summary>
        /// Uses the current display orientation to calculate the rotation transformation to apply to the face detection bounding box canvas
        /// and mirrors it if the preview is being mirrored
        /// </summary>
        private void SetFacesCanvasRotation()
        {
            // Calculate how much to rotate the canvas
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored, just like in SetPreviewRotationAsync
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Apply the rotation
            var transform = new RotateTransform { Angle = rotationDegrees };
            FacesCanvas.RenderTransform = transform;

            var previewArea = GetPreviewStreamRectInControl(_previewProperties as VideoEncodingProperties, PreviewControl);

            // For portrait mode orientations, swap the width and height of the canvas after the rotation, so the control continues to overlap the preview
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                FacesCanvas.Width = previewArea.Height;
                FacesCanvas.Height = previewArea.Width;

                // The position of the canvas also needs to be adjusted, as the size adjustment affects the centering of the control
                Canvas.SetLeft(FacesCanvas, previewArea.X - (previewArea.Height - previewArea.Width) / 2);
                Canvas.SetTop(FacesCanvas, previewArea.Y - (previewArea.Width - previewArea.Height) / 2);
            }
            else
            {
                FacesCanvas.Width = previewArea.Width;
                FacesCanvas.Height = previewArea.Height;

                Canvas.SetLeft(FacesCanvas, previewArea.X);
                Canvas.SetTop(FacesCanvas, previewArea.Y);
            }

            // Also mirror the canvas if the preview is being mirrored
            FacesCanvas.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        #region Face detection helpers

        /// <summary>
        /// Iterates over all detected faces, creating and adding Rectangles to the FacesCanvas as face bounding boxes
        /// </summary>
        /// <param name="faces">The list of detected faces from the FaceDetected event of the effect</param>
        private void HighlightDetectedFaces(IReadOnlyList<DetectedFace> faces)
        {
            // Remove any existing rectangles from previous events
            FacesCanvas.Children.Clear();

            if(faces == null || faces.Count < 1)
            {
                return;
            }

            double width = this.ActualWidth;
            double height = this.ActualHeight;
            GetEmotionDetails(width,height); 
            GetFaceDetails(width,height);
            // For each detected face
            for (int i = 0; i < faces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary
                Rectangle faceBoundingBox = ConvertPreviewToUiRectangle(faces[i].FaceBox);

                // Set bounding box stroke properties
                faceBoundingBox.StrokeThickness = 2;

                // Highlight the first face in the set
                faceBoundingBox.Stroke = (i == 0 ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.DeepSkyBlue));

                // Add grid to canvas containing all face UI objects
                FacesCanvas.Children.Add(faceBoundingBox);
            }

            // Update the face detection bounding box canvas orientation
            SetFacesCanvasRotation();
        }


        bool _emotionProcessing = false;

        public async void GetEmotionDetails(double width, double height)
        {
            if (_emotionProcessing)
            {
                return;
            }

            _emotionProcessing = true;

            try
            {
                Debug.WriteLine("Emotion Photo size : " + width + "x" + height);
                var jpgProperties = ImageEncodingProperties.CreateJpeg();
                jpgProperties.Width = (uint)width;
                jpgProperties.Height = (uint)height;

                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    if (_mediaCapture != null)
                    {
                        await _mediaCapture.CapturePhotoToStreamAsync(jpgProperties, randomAccessStream);
                        await randomAccessStream.FlushAsync();
                        randomAccessStream.Seek(0);

                        Emotion[] emotions = await _emotionServiceClient.RecognizeAsync(randomAccessStream.AsStream());

                        foreach (Emotion emotion in emotions)
                        {
                            if (emotion != null)
                            {
                                Scores scores = emotion.Scores;
                                if (scores != null)
                                {
                                    bool alreadyAdded = false;
                                    foreach (EmotionDetails existingEmotion in EmotionCollection)
                                    {
                                        if (existingEmotion.GetHashCode() == scores.GetHashCode())
                                        {
                                            alreadyAdded = true;
                                        }
                                    }
                                    if (!alreadyAdded)
                                    {
                                        Debug.WriteLine("Add hash : " + emotion.GetHashCode());
                                        EmotionCollection.Insert(0, EmotionDetails.FromEmotion(emotion));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Emotion ex : " + ex.Message);
            }

            _emotionProcessing = false;
        }

        bool _faceProcessing = false;

        public async void GetFaceDetails(double width, double height)
        {
            if(_faceProcessing)
            {
                return;
            }
            _faceProcessing = true;

            try
            {
                Debug.WriteLine("Face Photo size : " + width + "x" + height);
                var jpgProperties = ImageEncodingProperties.CreateJpeg();
                jpgProperties.Width = (uint)width;
                jpgProperties.Height = (uint)height;

                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    if (_mediaCapture != null)
                    {
                        await _mediaCapture.CapturePhotoToStreamAsync(jpgProperties, randomAccessStream);
                        await randomAccessStream.FlushAsync();
                        randomAccessStream.Seek(0);

                        Face[] faces = await _faceServiceClient.DetectAsync(randomAccessStream.AsStream(), true, false, new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.FacialHair, FaceAttributeType.Glasses });

                        foreach (Face face in faces)
                        {
                            if (face != null)
                            {
                                FaceAttributes attr = face.FaceAttributes;
                                if (attr != null)
                                {
                                    bool alreadyAdded = false;
                                    foreach (FaceDetails existingFace in FaceCollection)
                                    {
                                        if (existingFace.FaceId == face.FaceId)
                                        {
                                            alreadyAdded = true;
                                        }
                                    }
                                    if (!alreadyAdded)
                                    {
                                        Debug.WriteLine("Add id : " + face.FaceId);
                                        FaceCollection.Insert(0, FaceDetails.FromFace(face));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("face ex : " + ex.Message);
            }
            _faceProcessing = false;
        }

        /// <summary>
        /// Takes face information defined in preview coordinates and returns one in UI coordinates, taking
        /// into account the position and size of the preview control.
        /// </summary>
        /// <param name="faceBoxInPreviewCoordinates">Face coordinates as retried from the FaceBox property of a DetectedFace, in preview coordinates.</param>
        /// <returns>Rectangle in UI (CaptureElement) coordinates, to be used in a Canvas control.</returns>
        private Rectangle ConvertPreviewToUiRectangle(BitmapBounds faceBoxInPreviewCoordinates)
        {
            var result = new Rectangle();
            var previewStream = _previewProperties as VideoEncodingProperties;

            // If there is no available information about the preview, return an empty rectangle, as re-scaling to the screen coordinates will be impossible
            if (previewStream == null) return result;

            // Similarly, if any of the dimensions is zero (which would only happen in an error case) return an empty rectangle
            if (previewStream.Width == 0 || previewStream.Height == 0) return result;

            double streamWidth = previewStream.Width;
            double streamHeight = previewStream.Height;

            // For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamHeight = previewStream.Width;
                streamWidth = previewStream.Height;
            }

            // Get the rectangle that is occupied by the actual video feed
            var previewInUI = GetPreviewStreamRectInControl(previewStream, PreviewControl);

            // Scale the width and height from preview stream coordinates to window coordinates
            result.Width = (faceBoxInPreviewCoordinates.Width / streamWidth) * previewInUI.Width;
            result.Height = (faceBoxInPreviewCoordinates.Height / streamHeight) * previewInUI.Height;

            // Scale the X and Y coordinates from preview stream coordinates to window coordinates
            var x = (faceBoxInPreviewCoordinates.X / streamWidth) * previewInUI.Width;
            var y = (faceBoxInPreviewCoordinates.Y / streamHeight) * previewInUI.Height;
            Canvas.SetLeft(result, x);
            Canvas.SetTop(result, y);

            return result;
        }

        /// <summary>
        /// Calculates the size and location of the rectangle that contains the preview stream within the preview control, when the scaling mode is Uniform
        /// </summary>
        /// <param name="previewResolution">The resolution at which the preview is running</param>
        /// <param name="previewControl">The control that is displaying the preview using Uniform as the scaling mode</param>
        /// <returns></returns>
        public Rect GetPreviewStreamRectInControl(VideoEncodingProperties previewResolution, CaptureElement previewControl)
        {
            var result = new Rect();

            // In case this function is called before everything is initialized correctly, return an empty result
            if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
                previewResolution == null || previewResolution.Height == 0 || previewResolution.Width == 0)
            {
                return result;
            }

            var streamWidth = previewResolution.Width;
            var streamHeight = previewResolution.Height;

            // For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamWidth = previewResolution.Height;
                streamHeight = previewResolution.Width;
            }

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            result.Width = previewControl.ActualWidth;
            result.Height = previewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = previewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;

                result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = previewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;

                result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Registers event handlers for hardware buttons and orientation sensors, and performs an initial update of the UI rotation
        /// </summary>
        private void RegisterEventHandlers()
        {
            // If there is an orientation sensor present on the device, register for notifications
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
        }

        /// <summary>
        /// Unregisters event handlers for hardware buttons and orientation sensors
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        /// <summary>
        /// This event will fire when the page is rotated, when the DisplayInformation.AutoRotationPreferences value set in the SetupUiAsync() method cannot be not honored.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_previewProperties != null)
            {
                await SetPreviewRotationAsync();
            }
        }


        /// <summary>
        /// Occurs each time the simple orientation sensor reports a new sensor reading.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                // Only update the current orientation if the device is not parallel to the ground. This allows users to take pictures of documents (FaceUp)
                // or the ceiling (FaceDown) in portrait or landscape, by first holding the device in the desired orientation, and then pointing the camera
                // either up or down, at the desired subject.
                //Note: This assumes that the camera is either facing the same way as the screen, or the opposite way. For devices with cameras mounted
                //      on other panels, this logic should be adjusted.
                _deviceOrientation = args.Orientation;
            }
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

            await DeInit();
        }

        #endregion Event handlers

        #region Helper functions
        /// <summary>
        /// Attempts to find and return a device mounted on the panel specified, and on failure to find one it will return the first device listed
        /// </summary>
        /// <param name="desiredPanel">The desired panel on which the returned device should be mounted, if available</param>
        /// <returns></returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel, string name)
        {
            // Get available devices for capturing pictures
            DeviceInformationCollection  allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.Name.Contains(name));
            // Get the desired camera by panel
            if (desiredDevice == null)
            {
                desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            }
            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Calculates the current camera orientation from the device orientation by taking into account whether the camera is external or facing the user
        /// </summary>
        /// <returns>The camera orientation in space, with an inverted rotation in the case the camera is mounted on the device and is facing the user</returns>
        private SimpleOrientation GetCameraOrientation()
        {
            if (_externalCamera)
            {
                // Cameras that are not attached to the device do not rotate along with it, so apply no rotation
                return SimpleOrientation.NotRotated;
            }

            var result = _deviceOrientation;

            // Account for the fact that, on portrait-first devices, the camera sensor is mounted at a 90 degree offset to the native orientation
            if (_displayInformation.NativeOrientation == DisplayOrientations.Portrait)
            {
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        result = SimpleOrientation.NotRotated;
                        break;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated90DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated180DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.NotRotated:
                        result = SimpleOrientation.Rotated270DegreesCounterclockwise;
                        break;
                }
            }

            // If the preview is being mirrored for a front-facing camera, then the rotation should be inverted
            if (_mirroringPreview)
            {
                // This only affects the 90 and 270 degree cases, because rotating 0 and 180 degrees is the same clockwise and counter-clockwise
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts the given orientation of the app on the screen to the corresponding rotation in degrees
        /// </summary>
        /// <param name="orientation">The orientation of the app on the screen</param>
        /// <returns>An orientation in degrees</returns>
        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        #endregion Helper functions

    }
}
