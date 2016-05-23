using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceDetection.Controls
{
    public class FaceDetails : Face
    {
        public String Glasses { get; private set; }
        public String FacialHair { get; private set; }

        public String Smile { get; private set; }
        public static FaceDetails FromFace(Face face)
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

            return ret;
        }
    }
}
