using System;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Graphics.Imaging;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FaceDetection.Controls
{
    public class DataSender
    {
        public DataSender()
        {

        }

        public async void SendData(FaceWithEmotions[] faces, SoftwareBitmap image)
        {
            string imageString = "";
            if (image != null)
            {
                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);
                    encoder.SetSoftwareBitmap(image);
                    await encoder.FlushAsync();
                    randomAccessStream.Seek(0);

                    imageString = GetBase64EncodedString(randomAccessStream.AsStream());
                }
            }

            if (faces != null && faces.Length > 0)
            {
                JObject obj = faces[0].GetJsonObject(imageString);

                string json = obj.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        public string GetBase64EncodedString(Stream input)
        {
            byte[] buffer = new byte[input.Length];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return System.Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
