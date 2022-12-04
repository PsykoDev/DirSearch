using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace DirSearch
{
    internal class Program
    {
        static DirectoryInfo monrepertoire = new DirectoryInfo(Directory.GetCurrentDirectory() + "/Wordlist/");
        static string HostURI { get; set; }

        static string[] data;

        static int waitingForResponses = 0;

        static int maxQueriesAtOneTime = 200;

        static object consoleLock = new object();

        enum Scan
        {
            Dir,
            Dns
        }

        private static async void TaskCallBack(Object ThreadNumber)
        {
            for (int task = 0; task < data.Length; task++)
            {

                lock (consoleLock)
                {
                    if (!data[task].Contains("#") && !string.IsNullOrEmpty(data[task]))
                    {
                        int top = Console.CursorTop;

                        Console.CursorTop = 6;
                        Console.WriteLine("Scanning path: {0}    ", data[task]);

                        Console.CursorTop = top;
                    }
                }

                while (waitingForResponses >= maxQueriesAtOneTime)
                    Thread.Sleep(0);


                if (!data[task].Contains("#") && !string.IsNullOrEmpty(data[task]))
                {
                    Request(task);
                }
            }

            Console.WriteLine("Done!");
        }

        static void DecrementResponses()
        {
            Interlocked.Decrement(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        static void PrintWaitingForResponses()
        {
            lock (consoleLock)
            {
                int top = Console.CursorTop;

                Console.CursorTop = 8;
                Console.WriteLine("Waiting for responses from {0} request ", waitingForResponses);

                Console.CursorTop = top;
            }
        }

        static async Task Request(int task )
        {
            string tmpuri;
            if (HostURI.Contains("fuzz"))
            {
                tmpuri = HostURI;
                tmpuri = tmpuri.Replace("fuzz", data[task]);
            }
            else
            {
                tmpuri = HostURI;
                tmpuri = HostURI + data[task];
            }

            WebRequest request = WebRequest.Create(tmpuri);
            //Console.WriteLine(request.RequestUri);
            request.Method = "GET";
            request.Timeout = 10000;
            string test = String.Empty;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                        case HttpStatusCode.Accepted:
                        case HttpStatusCode.NonAuthoritativeInformation:
                        case HttpStatusCode.NoContent:
                        case HttpStatusCode.ResetContent:
                        case HttpStatusCode.PartialContent:
                        case HttpStatusCode.MultiStatus:
                        case HttpStatusCode.AlreadyReported:
                        case HttpStatusCode.IMUsed:
                        case HttpStatusCode.MultipleChoices:
                        case HttpStatusCode.Found:
                        case HttpStatusCode.SeeOther:
                        case HttpStatusCode.UseProxy:
                        case HttpStatusCode.TemporaryRedirect:
                        case HttpStatusCode.PermanentRedirect:
                        case HttpStatusCode.Moved:
                            lock (consoleLock)
                            {
                                Console.WriteLine($"\t{data[task]} [{(int)response.StatusCode}] => {tmpuri}");
                                Interlocked.Increment(ref waitingForResponses);
                            }
                            break;
                        case HttpStatusCode.NotFound:
                            lock (consoleLock)
                            {
                                Console.WriteLine("404");
                                Interlocked.Increment(ref waitingForResponses);
                            }
                            break;
                        default:
                            Interlocked.Increment(ref waitingForResponses);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref waitingForResponses);
            }

            DecrementResponses();
        }
        static void Main(string[] args)
        {
            HostURI = args[0];
            if (!HostURI.Contains("fuzz"))
            {
                Console.WriteLine("add fuzz depending scan type Dir or Dns");
                Console.WriteLine("Ex : https://exemple.com/fuzz dir");
                Console.WriteLine("Ex : https://fuzz.exemple.com/ dns");
            }
            else
            {
                switch (args[1].ToLower())
                {
                    case "dir":
                        data = File.ReadAllLines(Directory.GetFiles(monrepertoire.ToString())[0]);
                        break;
                    case "dns":
                        data = File.ReadAllLines(Directory.GetFiles(monrepertoire.ToString())[1]);
                        break;
                }
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallBack));
                Thread.Sleep(-1);
            }
        }
    }
}