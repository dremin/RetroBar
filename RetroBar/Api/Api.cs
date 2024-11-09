using System;
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
        listener.Prefixes.Add ($"http://localhost:{Settings.Instance.ApiPort}/");
        listener.Start();
        Task.Run(async () =>
        {
            while (true) // only loops once per api call
            {
                HttpListenerContext context = await listener.GetContextAsync(); // GetContextAsync waits for an api call
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                response.StatusCode = 200;
                using StreamReader reader = new(request.InputStream, request.ContentEncoding);
                string jsonPatchString = await reader.ReadToEndAsync();
                var operations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Operation>>(jsonPatchString);
                JsonPatchDocument patch = new(operations, new DefaultContractResolver());
                try
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        patch.ApplyTo(Settings.Instance);
                    });
                }
                catch (Exception e)
                {
                    response.StatusCode = 401;
                    using StreamWriter writer = new(response.OutputStream, response.ContentEncoding);
                    writer.Write(e.Message);
                }
                response.Close();
            }
        });
    }
}