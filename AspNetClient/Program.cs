using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;
using Shared;

namespace AspNetClient
{
	class Program
	{
        //private const string _BASE_URL = "https://localhost:5001/";
        private const string _BASE_URL = "http://localhost:54890/api/Files/";

        static void Main(string[] args)
		{
			var messageHandler = new HttpClientHandler()
			{
				SslProtocols = SslProtocols.Tls12,
				ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
			};
            //using (var client = new HttpClient(messageHandler))
            using (var client = new HttpClient())
            {
				client.BaseAddress = new Uri(_BASE_URL);
				var bytes = Helpers.GetRandomBytes(20);

				//PingServer(client).Wait();
                //Upload(client, bytes, false).Wait();
                //Upload(client, bytes, true).Wait();
                //UploadMultipart(client, bytes, false).Wait();
                //UploadMultipart(client, bytes, true).Wait();
                //Download(client, false).Wait();
                //Download(client, true).Wait();
                //UploadSql(client).Wait();
                DownloadSql(client,4).Wait();
            }
            Console.ReadKey();
            Console.WriteLine("Press any key to exit...");
		}

		private static async Task PingServer(HttpClient client)
		{
			var sw = Stopwatch.StartNew();

			using (var response = await client.GetAsync(""))
			{
				var result = await response.Content.ReadAsStringAsync();
				Console.WriteLine(result);
			}

			sw.Stop();
			Console.WriteLine($"Ping took {sw.Elapsed}");
		}

		private static async Task Upload(HttpClient client, byte[] bytes, bool readFromFs)
		{
			var filePath = "./File.bin";

			if (readFromFs)
				File.WriteAllBytes(filePath, bytes);

			try
			{
				var sw = Stopwatch.StartNew();

				var stream = readFromFs
					? (Stream) File.OpenRead(filePath)
					: new MemoryStream(bytes);

				using (var content = new StreamContent(stream, 128 * 1024))
				{
					using (var response = await client.PostAsync("Upload", content))
					{
						response.EnsureSuccessStatusCode();
					}
				}

				sw.Stop();
				Helpers.PrintDuration("Upload from " + (readFromFs ? "File System" : "Memory"), bytes.Length, sw.Elapsed);
			}
            catch(Exception ex)
            {

            }
			finally
			{
				if (readFromFs)
					File.Delete(filePath);
			}
		}

        private static async Task UploadSql(HttpClient client)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var filePath = @"C:\Users\matth\Desktop\Xmas.png";
                var bytes = File.ReadAllBytes(filePath);

                using ( var content = new MultipartFormDataContent())
                {
                    content.Add(new StreamContent(new MemoryStream(bytes))
                    {
                        Headers =
                    {
                        ContentLength = bytes.Length,
                        ContentType = new MediaTypeHeaderValue("application/octet-stream")
                    }
                    }, "File", "Xmas.png");
                    content.Add(new StringContent(42.ToString()), "SomeValue");

                    using (var response = await client.PostAsync("UploadSql", content))
                    {
                        await response.Content.ReadAsStreamAsync();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static async Task UploadMultipart(HttpClient client, byte[] bytes, bool useReader)
		{
			var sw = Stopwatch.StartNew();

			using (var content = new MultipartFormDataContent())
			{
				content.Add(new StreamContent(new MemoryStream(bytes))
				{
					Headers =
					{
						ContentLength = bytes.Length,
						ContentType = new MediaTypeHeaderValue("application/octet-stream")
					}
				}, "File", "File.bin");
				content.Add(new StringContent(42.ToString()), "SomeValue");

				using (var response = await client.PostAsync((useReader ? "UploadMultipartUsingReader" : "UploadMultipartUsingIFormFile"), content))
				{
					await response.Content.ReadAsStreamAsync();
				}
			}

			sw.Stop();
			Helpers.PrintDuration("UploadMultipart using " + (useReader ? "Multipart-Reader" : "IFormFile"), bytes.Length, sw.Elapsed);
		}

		private static async Task Download(HttpClient client, bool useCustomFileResult)
		{
			var sw = Stopwatch.StartNew();
			int totalBytes;

			using (var response = await client.GetAsync("Download?useCustomFileResult=" + useCustomFileResult))
			{
				var stream = await response.Content.ReadAsStreamAsync();
				var bufferSize = 4 * 1024;
				totalBytes = await Helpers.ReadStream(stream, bufferSize);
			}

			sw.Stop();
			Helpers.PrintDuration("Download using " + (useCustomFileResult ? "CustomFileResult" : "Default-FileResult"), totalBytes, sw.Elapsed);
		}

        private static async Task DownloadSql(HttpClient client, int id)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using (var response = await client.GetAsync($"Download/sql/{id}"))
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    byte[] buffer = new byte[64 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        var bytes = ms.ToArray();
                        var filePath = @"C:\Users\matth\Desktop\Xmas3.png";
                        await File.WriteAllBytesAsync(filePath, bytes);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            sw.Stop();
        }
    }
}