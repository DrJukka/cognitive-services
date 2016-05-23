using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Diagnostics;

namespace FaceDetection.Controls
{
    public class EmotionDetails
    {
        private Dictionary<string, float> _moods = new Dictionary<string, float>();
        public String Mood1 { get; set;}
        public String Mood2 { get; set; }
        public String Mood3 { get; set; }

        public static EmotionDetails FromEmotion(Emotion emotion)
        {
            EmotionDetails ret = new EmotionDetails();

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

            return ret;
        }
    }
}
