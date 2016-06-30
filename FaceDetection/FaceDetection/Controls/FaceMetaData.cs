using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;

using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml.Media.Imaging;

namespace FaceDetection.Controls
{
    public delegate void DetectedFaces(FaceWithEmotions[] faces);

    public class FaceMetaData
    {
        public DetectedFaces DetectedFaces;
        /// <summary>
        /// Face service client
        /// </summary>
        private IFaceServiceClient _faceServiceClient = null;
        private EmotionServiceClient _emotionServiceClient = null;

        private bool _processingFace = false;
        private ThreadPoolTimer _threadPoolTimer;
        private List<Guid> _seenAlready;
        public FaceMetaData(string facekey, string emotionKey)
        {
            _seenAlready = new List<Guid>();
            // Initialize the face service client
            _faceServiceClient = new FaceServiceClient(facekey);
            _emotionServiceClient = new EmotionServiceClient(emotionKey);
        }

        public void Close()
        {
            if (_threadPoolTimer != null)
            {
                _threadPoolTimer.Cancel();
                _threadPoolTimer = null;
            }
        }

        /// <summary>
        /// Upload the frame and get the face detect result
        /// </summary>
        /// 

        public async void DetectFaces(SoftwareBitmap bitmap)
        {
            if (bitmap == null || _processingFace)
            {
                return;
            }
            _processingFace = true;
            try
            {
                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);
                    encoder.SetSoftwareBitmap(bitmap);
                    await encoder.FlushAsync();
                    randomAccessStream.Seek(0);

                    Face[] detectedfaces = await _faceServiceClient.DetectAsync(randomAccessStream.AsStream(), true, false, new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.FacialHair, FaceAttributeType.Glasses });

                    CheckPersons(detectedfaces, bitmap);
                }
            }
            catch (Exception ex)
            {
             //   Debug.WriteLine("FaceAPIException HttpStatus: " + ex.HttpStatus + ", ErrorCode : " + ex.ErrorCode + ", ErrorMessage: " + ex.ErrorMessage);
                Debug.WriteLine("DetectFaces exception : " + ex.Message);
                ProcessResults(null, null, null);
            }
        }

        private async void CheckPersons(Face[] faces, SoftwareBitmap bitmap)
        {
            if (faces != null && bitmap != null)
            {
                List<Face> newFaces = new List<Face>();

                try
                {
                    foreach (Face face in faces)
                    {
                        if (face != null)
                        {
                            newFaces.Add(face);
                            //Current Face API key only allows 20 calls per minute
                            //so we can not use the Vefification at this point of time
                          /*  foreach (Guid guid in _seenAlready)
                            {
                                VerifyResult result = await _faceServiceClient.VerifyAsync(face.FaceId, guid);
                                if (result.IsIdentical || result.Confidence > 0.5)
                                {
                                    newFaces.Remove(face);
                                }
                            }*/
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CheckPersons exception : " + ex.ToString());
                }

                if(newFaces.Count > 0)
                {
                    GetEmotionDetails(bitmap, newFaces);
                }
                else
                {
                    ProcessResults(null,null,null);
                }
            }
        }
        private async void GetEmotionDetails(SoftwareBitmap bitmap, List<Face> faces)
        {
            if (bitmap != null)
            {
                try
                {
                    using (var randomAccessStream = new InMemoryRandomAccessStream())
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);
                        encoder.SetSoftwareBitmap(bitmap);
                        await encoder.FlushAsync();
                        randomAccessStream.Seek(0);

                        Emotion[] emotions = await _emotionServiceClient.RecognizeAsync(randomAccessStream.AsStream());
                        ProcessResults(faces?.ToArray(), emotions?.ToArray(), bitmap);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetEmotionDetails exception : " + ex.Message);
                    ProcessResults(faces?.ToArray(), null, bitmap);
                }
            }
        }

        private async void ProcessResults(Face[] faces, Emotion[] emotions, SoftwareBitmap swbitmap)
        {
            if (faces != null || swbitmap != null)
            {
                try
                {
                    WriteableBitmap bitmap = new WriteableBitmap(swbitmap.PixelWidth, swbitmap.PixelHeight);
                    swbitmap.CopyToBuffer(bitmap.PixelBuffer);

                    List<FaceWithEmotions> result = new List<FaceWithEmotions>();
                    foreach (Face person in faces)
                    {
                        _seenAlready.Add(person.FaceId);
                        Emotion personEmotion = null;
                        int currentMinimum = 65000;
                        if (emotions != null)
                        {
                            foreach (Emotion emo in emotions)
                            {
                                int diff = RectIntersectDifference(person.FaceRectangle, emo.FaceRectangle);
                                if (diff < currentMinimum)
                                {
                                    currentMinimum = diff;
                                    personEmotion = emo;
                                }
                            }
                        }
                        
                        WriteableBitmap img = await CropAsync(bitmap, IncreaseSize(person.FaceRectangle, bitmap.PixelHeight, bitmap.PixelWidth));
                        result.Add(new FaceWithEmotions(person, personEmotion, img));
                    }

                    while (_seenAlready.Count > 10)
                    {
                        _seenAlready.RemoveAt(0);
                    }

                    DetectedFaces?.Invoke(result.ToArray());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ProcessResults exception : " + ex.Message);
                }
            }

            //_processingFace = false;

            // to avoid asking too many queries, the limit is 20 per minute
            // lets have a quick break after each query
            if (_threadPoolTimer == null)
            {
                _threadPoolTimer = ThreadPoolTimer.CreateTimer((source) =>
                {
                    _processingFace = false;
                    _threadPoolTimer = null;
                },
                TimeSpan.FromMilliseconds(3500));
            }
        }

        private FaceRectangle IncreaseSize(FaceRectangle face, Int32 imageHeight, Int32 imageWidth)
        {
            int Left   = face.Left - (face.Width / 6);
            if(Left < 0)
            {
                Left = 0;
            }
            int Top    = face.Top - (face.Height / 3);
            if(Top < 0)
            {
                Top = 0;
            }
            int Height = face.Height + (face.Height / 2);
            if(Height > (imageHeight - Top))
            {
                Height = (imageHeight - Top);
            }
            int Width  = face.Width + (face.Width / 3);
            if (Width > (imageWidth - Left))
            {
                Width = (imageWidth - Left);
            }

            FaceRectangle newRect = new FaceRectangle();
            newRect.Left = Left;
            newRect.Top = Top;
            newRect.Height = Height;
            newRect.Width = Width;

            return newRect;
        }
        private int RectIntersectDifference(FaceRectangle face, Rectangle emotion)
        {
            Rect faceRect = new Rect(new Point(face.Left, face.Top),new Size(face.Width, face.Height));
            faceRect.Intersect(new Rect(new Point(emotion.Left, emotion.Top), new Size(emotion.Width, emotion.Height)));

            return (int)(face.Width - faceRect.Width) + (int)(face.Height - faceRect.Height);       
        }

        public async Task<WriteableBitmap> CropAsync(WriteableBitmap originalImage, FaceRectangle rect)
        {
            WriteableBitmap CroppedImage = null;

            double x = rect.Left;
            double y = rect.Top;
            double width = rect.Width;
            double height = rect.Height;

            x = Math.Round(x, 0);
            y = Math.Round(y, 0);
            width = Math.Round(width, 0);
            height = Math.Round(height, 0);

            byte[] originalImagePixelArray = originalImage.PixelBuffer.ToArray();
            int bytesPerPixel = (int)Math.Round((double)originalImagePixelArray.Length / (originalImage.PixelWidth * originalImage.PixelHeight), 0);
            int croppedImagePixelArrayLength = (int)width * (int)height * bytesPerPixel;
            int offset = ((y - 1 >= 0) ? (int)((y - 1) * originalImage.PixelWidth * bytesPerPixel) : 0);
            byte[] croppedImagePixelArray = new byte[croppedImagePixelArrayLength];
            int croppedImagePixelArrayIndex = 0;
            for (int line = 0; line < (int)height; ++line)
            {
                offset += (int)x * bytesPerPixel;
                for (int i = 0; i < (int)(width * bytesPerPixel); ++i)
                {
                    croppedImagePixelArray[croppedImagePixelArrayIndex++] = originalImagePixelArray[offset + i];
                }
                offset += (int)(originalImage.PixelWidth - x) * bytesPerPixel;
            }
            CroppedImage = new WriteableBitmap((int)width, (int)height);
            using (Stream stream = CroppedImage.PixelBuffer.AsStream())
            {
                if (stream.CanWrite)
                {
                    await stream.WriteAsync(croppedImagePixelArray, 0, croppedImagePixelArray.Length);
                    stream.Flush();
                }
            }

            return CroppedImage;
        }
    }
}
