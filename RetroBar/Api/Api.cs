using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Serialization;
using RetroBar.Utilities;

namespace RetroBar.Api;

public class Api
{
    public static void StartListening()
    {
        HttpListener listener = new();
        listener.Prefixes.Add ("http://localhost:51111/"); // Listen on 51111
        listener.Start();
        Task.Run(async () =>
        {
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                using (StreamReader reader = new(request.InputStream, request.ContentEncoding))
                {
                    string jsonPatchString = await reader.ReadToEndAsync();
                    var operations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Operation>>(jsonPatchString);
                    JsonPatchDocument patch = new(operations, new DefaultContractResolver());
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        patch.ApplyTo(Settings.Instance);
                    });
                }
                HttpListenerResponse response = context.Response;
                response.StatusCode = 200;
                response.Close();
            }
        });
    }
}