using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

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
        public FaceWithEmotions(Face face, Emotion emotion)
        {
            Face = face;
            Emotion = emotion;
        }

        public JObject GetJsonObject(string image)
        {
            dynamic jsonObject = new JObject();

            jsonObject.Add("image", image);//Base64Encoded jpg image

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

            if(Emotion != null && Emotion.Scores != null)
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
    }
}
