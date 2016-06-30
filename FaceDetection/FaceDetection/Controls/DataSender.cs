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
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceDetection.Controls
{
    public class DataSender
    {
        private HttpClient _Client;
        private const string SERVER_URL = "http://localhost:8000/Phaser";
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

        public async Task SendData(FaceWithEmotions[] faces)
        {
            if (_Client == null)
            {
                return;
            }

            if (faces != null && faces.Length > 0)
            {
                foreach (FaceWithEmotions face in faces)
                {
                    if (face != null && face.Face != null)
                    {
                        JObject obj = await face.GetJsonObject();
                        await DoPostData(SERVER_URL, obj);
                    }
                }
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
