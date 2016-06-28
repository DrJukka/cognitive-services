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
using Windows.Foundation;
using Windows.Graphics.Imaging;

using Windows.Storage.Streams;
using Windows.System.Threading;


namespace FaceDetection.Controls
{
    public delegate void DetectedFaces(FaceWithEmotions[] faces, SoftwareBitmap image);

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
            catch (FaceAPIException ex)
            {
                Debug.WriteLine("FaceAPIException HttpStatus: " + ex.HttpStatus + ", ErrorCode : " + ex.ErrorCode + ", ErrorMessage: " + ex.ErrorMessage);
                Debug.WriteLine("DetectFaces exception : " + ex.ToString());
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

        // to avoid asking too many queries, lets have a quick break after each query
        private void ProcessResults(Face[] faces, Emotion[] emotions, SoftwareBitmap bitmap)
        {
            if (faces != null || bitmap != null)
            {
                try
                {
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

                        result.Add(new FaceWithEmotions(person, personEmotion));
                    }

                    while(_seenAlready.Count > 10)
                    {
                        _seenAlready.RemoveAt(0);
                    }

                    DetectedFaces?.Invoke(result.ToArray(), bitmap);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ProcessResults exception : " + ex.Message);
                }
            }

            //_processingFace = false;

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

        private int RectIntersectDifference(FaceRectangle face, Rectangle emotion)
        {
            Rect faceRect = new Rect(new Point(face.Left, face.Top),new Size(face.Width, face.Height));
            faceRect.Intersect(new Rect(new Point(emotion.Left, emotion.Top), new Size(emotion.Width, emotion.Height)));

            return (int)(face.Width - faceRect.Width) + (int)(face.Height - faceRect.Height);       
        }
    }
}
