using System;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Graphics.Imaging;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace FaceDetection.Controls
{
    public class DataSender
    {
        private HttpClient _Client;
        private const string SERVER_URL = "http://www.drjukka.com";
        public DataSender()
        {
            _Client = new HttpClient();
            _Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public void Close()
        {
            if (_Client != null)
            {
                _Client.Dispose();
                _Client = null;
            }
        }

        public async Task SendData(FaceWithEmotions[] faces, SoftwareBitmap image)
        {
            if (_Client == null)
            {
                return;
            }

            if (faces != null && faces.Length > 0)
            {
                string imageString = await Base64Image(image);

                foreach (FaceWithEmotions face in faces)
                {
                    if (face != null)
                    {
                        JObject obj = face.GetJsonObject(imageString);
                        await DoPostData(SERVER_URL, obj);
                    }
                }
            }
        }
        public async Task<String> Base64Image(SoftwareBitmap image)
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

        private async Task DoPostData(string url, JObject obj)
        {
            try
            {
                HttpResponseMessage httpResponse = await _Client.PostAsync(url, new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
                httpResponse.EnsureSuccessStatusCode();
                Debug.WriteLine("DataSender - Response.StatusCode  : " + httpResponse.StatusCode);
               // Debug.WriteLine("DataSender - Response : " + await httpResponse.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DataSender - DoPostData : " + ex.Message);
            }
        }
    }
}
