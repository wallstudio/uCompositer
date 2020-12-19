using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace uCompositer
{
    static class Program
    {
        static readonly Guid session = Guid.NewGuid();
     
        static void Main(string[] args)
        {
            Console.Write($"Metafile path? :");
            var file = args.ElementAtOrDefault(0) ?? Console.ReadLine().Trim("\"\t ".ToCharArray());
            Console.WriteLine($"Download by metafile {file}");
            var dstFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", Path.GetFileNameWithoutExtension(file));
            try
            {
                switch(File.ReadAllLines(file).First())
                {
                    case "#EXTM3U":
                        ConcatM3u8(file, dstFile, Console.WriteLine);
                        break;
                    default:
                        var dlFiles = File.ReadAllLines(file).Select((line, i) =>
                        {
                            var url = line.Split(" ").First();
                            var ext = $"." + (line.Split(" ").ElementAtOrDefault(1) ?? "webm");
                            var dst = Path.Combine(Path.GetTempPath(), session + $"_{i}{ext}"); ;
                            Download(url, dst);
                            return dst;
                        }).ToArray();
                        try
                        {
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
                        break;
                }
            }
            catch (Exception e)
            {
                File.WriteAllText(file + ".error", e.ToString());
                Console.Error.WriteLine(e);
                Console.ReadKey();
            }
        }

        class StandartErrorException : Exception
        {
            public StandartErrorException(string msg) : base(msg) {}
        }

        static void MergeMedias(string[] mediaFiles, string dstFile, Action<string> onStdout)
            => FfmpegCommand($"-y {mediaFiles.Select(f => $"-i \"{f}\"").ToStringJoin(" ")} -c copy \"{dstFile}.mkv\"", _ => {}, _ => {});

        static void ConcatM3u8(string file, string dstFile, Action<string> onStdout)
            => FfmpegCommand($"-protocol_whitelist file,http,https,tcp,tls,crypto  -i \"{file}\" -c copy \"{dstFile}\".mp4", onStdout, onStdout);

        static void FfmpegCommand(string arguments, Action<string> onStdout, Action<string> onStderror)
        {
            var pInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Console.WriteLine($"{pInfo.FileName} {pInfo.Arguments}");
            var p = Process.Start(pInfo);

            Task.Run(() =>
            {
                while (!p.HasExited)
                {
                    onStdout?.Invoke(p.StandardOutput.ReadLine());
                }
            });
            var error = new StringBuilder();
            Task.Run(() =>
            {
                while (!p.HasExited)
                {
                    var line = p.StandardError.ReadLine();
                    error.AppendLine(line);
                    onStderror?.Invoke(line);
                }
            });
            p.WaitForExit();
            if(p.ExitCode != 0)
            {
                throw new StandartErrorException($"{p.ExitCode}\n\n{error}");
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
