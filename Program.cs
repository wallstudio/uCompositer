using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace uCompositer
{
    static class Program
    {
        static readonly Guid session = Guid.NewGuid();
     
        static void Main(string[] args)
        {
            try
            {
                if(args?.Length < 1)
                {
                    Console.Write($"Metafile path? :");
                    args = new [] { Console.ReadLine().Trim() };
                }
                Console.WriteLine($"Download by metafile {args[0]}");

                var dlFiles = File.ReadAllLines(args[0]).Select((line, i) =>
                {
                    var url = line.Split(" ").First();
                    var ext = $"." + (line.Split(" ").ElementAtOrDefault(1) ?? "webm");
                    var dst = Path.Combine(Path.GetTempPath(), session + $"_{i}{ext}"); ;
                    Download(url, dst);
                    return dst;
                }).ToArray();

                try
                {
                    var dstFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads", Path.GetFileNameWithoutExtension(args[0]));
                    MergeMedias(dlFiles, dstFile, Console.WriteLine);
                    Console.WriteLine("Completed!");
                    for (int i = 5; i >= 0; i--)
                    {
                        Console.WriteLine(i);
                        Task.Delay(1000).Wait();
                    }
                    dlFiles.ToList().ForEach(f => File.Delete(f));
                }
                catch(StandartErrorException)
                {
                    Process.Start("explorer", $"/select,\"{dlFiles.First()}\"");
                    throw;
                }
            }
            catch (Exception e)
            {
                File.WriteAllText(args[0] + ".error", e.ToString());
                Console.Error.WriteLine(e);
                Console.ReadKey();
            }
        }

        class StandartErrorException : Exception
        {
            public StandartErrorException(string msg) : base(msg) {}
        }

        static void MergeMedias(string[] mediaFiles, string dstFile, Action<string> onStdout)
        {
            var pInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = $"-y {mediaFiles.Select(f => $"-i \"{f}\"").ToStringJoin(" ")} -c copy \"{dstFile}.mkv\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Console.WriteLine($"{pInfo.FileName} {pInfo.Arguments}");
            var p = Process.Start(pInfo);

            while (!p.HasExited)
            {
                onStdout?.Invoke(p.StandardOutput.ReadLine());
            }

            var error = p.StandardError.ReadToEnd();
            if(string.IsNullOrEmpty(error.Trim()))
            {
                throw new StandartErrorException(error);
            }
        }

        static void Download(string url, string dst)
        {
            Console.WriteLine($"Download {url}");
            Console.WriteLine($"Download {dst}");

            using (var cli = new HttpClient())
            using (var res = cli.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                HttpCompletionOption.ResponseHeadersRead)
                .WaitResult())
            using (var stream = res.Content.ReadAsStreamAsync().WaitResult())
            using (var sw = new FileStream(dst, FileMode.Create, FileAccess.ReadWrite))
            {
                res.Headers.TryGetValues("Content-Length", out var contentLengths);
                int.TryParse(contentLengths?.FirstOrDefault() ?? "0", out var size);
                {
                    var buf = new byte[256 * 1024 * 1024];
                    var total = 0;
                    var prevTotal = 0;
                    var c = 0;
                    for (; ; )
                    {
                        c = stream.Read(buf, 0, buf.Length);
                        if (c <= 0)
                        {
                            break;
                        }
                        total += c;
                        sw.Write(buf, 0, c);
                        if ((total - prevTotal) / 1024 / 1024 > 0)
                        {
                            Console.CursorLeft = 0;
                            Console.Write($"{(total / 1024 / 1024):#,0} MB / {size / 1024 / 1024} MB");
                            prevTotal = total;
                        }
                    }
                }
            }

            Console.WriteLine();
        }
            
        static T WaitResult<T>(this Task<T> t)
        {
            t.Wait();
            return t.Result;
        }

        static string ToStringJoin<T>(this IEnumerable<T> collection, string separator) => string.Join(separator, collection.Select(e => e.ToString()));
    }
}
