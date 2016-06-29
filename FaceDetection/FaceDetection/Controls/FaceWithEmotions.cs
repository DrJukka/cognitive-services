using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FaceDetection.Controls
{
    public class FaceWithEmotions
    {
        public Face Face {
            get;
            private set;
        }
        public Emotion Emotion {
            get;
            private set;
        }

        public WriteableBitmap Bitmap
        {
            get;
            private set;
        }
     
        public FaceWithEmotions(Face face, Emotion emotion, WriteableBitmap bitmap)
        {
            Face = face;
            Emotion = emotion;
            Bitmap = bitmap;
        }

        public async Task<JObject> GetJsonObject()
        {
            dynamic jsonObject = new JObject();

            string imageString = await Base64Image(Bitmap);
            //int yyy = imageString.Length;

            jsonObject.Add("image", imageString);//Base64Encoded jpg image

            if (Face != null)
            {
                if (Face.FaceRectangle != null)
                {
                    jsonObject.faceRectangle = new JObject();
                    jsonObject.faceRectangle.Add("width", Face.FaceRectangle.Width);
                    jsonObject.faceRectangle.Add("height", Face.FaceRectangle.Height);
                    jsonObject.faceRectangle.Add("left", Face.FaceRectangle.Left);
                    jsonObject.faceRectangle.Add("top", Face.FaceRectangle.Top);
                }

                if (Face.FaceAttributes != null)
                {
                    jsonObject.faceAttributes = new JObject();
                    jsonObject.faceAttributes.Add("age", Face.FaceAttributes.Age);
                    jsonObject.faceAttributes.Add("gender", Face.FaceAttributes.Gender);
                    jsonObject.faceAttributes.Add("smile", Face.FaceAttributes.Smile);

                    switch (Face.FaceAttributes.Glasses)
                    {
                        case Glasses.NoGlasses:
                            jsonObject.faceAttributes.Add("glasses", "No glasses");
                            break;
                        case Glasses.ReadingGlasses:
                            jsonObject.faceAttributes.Add("glasses", "Reading glasses");
                            break;
                        case Glasses.Sunglasses:
                            jsonObject.faceAttributes.Add("glasses", "Sun glasses");
                            break;
                        case Glasses.SwimmingGoggles:
                            jsonObject.faceAttributes.Add("glasses", "Swimming goggles");
                            break;
                    }

                    if (Face.FaceAttributes.HeadPose != null)
                    {
                        jsonObject.faceAttributes.HeadPose = new JObject();
                        jsonObject.faceAttributes.HeadPose.Add("roll", Face.FaceAttributes.HeadPose.Roll);
                        jsonObject.faceAttributes.HeadPose.Add("yaw", Face.FaceAttributes.HeadPose.Yaw);
                        jsonObject.faceAttributes.HeadPose.Add("pitch", Face.FaceAttributes.HeadPose.Pitch);
                    }

                    if (Face.FaceAttributes.FacialHair != null)
                    {
                        jsonObject.faceAttributes.FacialHair = new JObject();
                        jsonObject.faceAttributes.FacialHair.Add("moustache", Face.FaceAttributes.FacialHair.Moustache);
                        jsonObject.faceAttributes.FacialHair.Add("beard", Face.FaceAttributes.FacialHair.Beard);
                        jsonObject.faceAttributes.FacialHair.Add("sideburns", Face.FaceAttributes.FacialHair.Sideburns);
                    }
                }
            }

            if (Emotion != null && Emotion.Scores != null)
            {
                jsonObject.scores = new JObject();
                jsonObject.scores.Add("anger", Emotion.Scores.Anger);
                jsonObject.scores.Add("contempt", Emotion.Scores.Contempt);
                jsonObject.scores.Add("disgust", Emotion.Scores.Disgust);
                jsonObject.scores.Add("fear", Emotion.Scores.Fear);
                jsonObject.scores.Add("happiness", Emotion.Scores.Happiness);
                jsonObject.scores.Add("neutral", Emotion.Scores.Neutral);
                jsonObject.scores.Add("sadness", Emotion.Scores.Sadness);
                jsonObject.scores.Add("surprise", Emotion.Scores.Surprise);
            }
            return jsonObject;
        }

        private async Task<String> Base64Image(WriteableBitmap image)
        {
            string imageString = "";
            if (image != null)
            {
                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                        (uint)image.PixelWidth,
                                        (uint)image.PixelHeight,
                                        96.0,
                                        96.0,
                                        pixels);
                    await encoder.FlushAsync();
                    randomAccessStream.Seek(0);

                    imageString = GetBase64EncodedString(randomAccessStream.AsStream());
                }
            }

            return imageString;
        }
        private string GetBase64EncodedString(Stream input)
        {
            byte[] buffer = new byte[input.Length];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
