using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Emotion.Contract;

namespace FaceDetection.Controls
{
    public class FaceDetails : Face
    {
        private Dictionary<string, float> _moods = new Dictionary<string, float>();
        public String Mood1 { get; set; }
        public String Mood2 { get; set; }
        public String Mood3 { get; set; }

        public String Glasses { get; private set; }
        public String FacialHair { get; private set; }

        public String Smile { get; private set; }
        public static FaceDetails FromFaceAndEmotion(Face face, Emotion emotion)
        {
            FaceDetails ret = new FaceDetails();
            ret.FaceAttributes = face.FaceAttributes;
            ret.FaceId = face.FaceId;
            ret.FaceLandmarks = face.FaceLandmarks;
            ret.FaceRectangle = face.FaceRectangle;

            switch ((int)face.FaceAttributes.Glasses) {
                case 1:// Glasses.Sunglasses:
                    ret.Glasses = "Sunglasses";
                    break;
                case 2:// Glasses.ReadingGlasses:
                    ret.Glasses = "Glasses";
                    break;
                case 3:// Glasses.SwimmingGoggles:
                    ret.Glasses = "SwimmingGoggles";
                    break;
                default:
                    break;
            }

            if (face.FaceAttributes.FacialHair.Beard > 0)
            {
                ret.FacialHair = "Beard";
            }
            else
            {
                if (face.FaceAttributes.FacialHair.Moustache > 0)
                {
                    ret.FacialHair = "Moustache";
                }
                else
                {
                    if (face.FaceAttributes.FacialHair.Sideburns > 0)
                    {
                        ret.FacialHair = "Sideburns";
                    }
                }
            }

            ret.Smile = "Smile Value " + face.FaceAttributes.Smile;

            if(emotion != null)
            {
                ret._moods.Add("Anger", emotion.Scores.Anger);
                ret._moods.Add("Contempt", emotion.Scores.Contempt);
                ret._moods.Add("Disgust", emotion.Scores.Disgust);
                ret._moods.Add("Fear", emotion.Scores.Fear);
                ret._moods.Add("Happiness", emotion.Scores.Happiness);
                ret._moods.Add("Neutral", emotion.Scores.Neutral);
                ret._moods.Add("Sadness", emotion.Scores.Sadness);
                ret._moods.Add("Surprise", emotion.Scores.Surprise);

                List<KeyValuePair<string, float>> sorted = (from kv in ret._moods orderby kv.Value select kv).ToList();

                ret.Mood1 = sorted[7].Key;
                ret.Mood2 = sorted[6].Key;
                ret.Mood3 = sorted[5].Key;
            }

            return ret;
        }
    }
}
