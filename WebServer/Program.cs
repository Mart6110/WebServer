using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
{
    internal class Program
    {
        // TCP listener for incoming connections
        private static TcpListener tcpListener;

        // Port on which the server will listen for incoming connections
        private static int port = 5050;

        // IP address on which the server will listen for incoming connections
        private static IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        // Path to the web server's root directory
        private static string WebServerPath = @"C:\Users\Mart6110\source\repos\WebServer\WebServer\Web-Server";

        // ETag (Entity Tag) for cache validation
        private static string serverEtag = Guid.NewGuid().ToString("N");

        // Entry point of the application
        static void Main(string[] args)
        {
            try
            {
                // Create and start the TCP listener
                tcpListener = new TcpListener(localAddr, port);
                tcpListener.Start();

                // Display a message indicating that the web server is running
                Console.WriteLine($"Web Server Running on {localAddr.ToString()} on port {port}...");

                // Create a new thread to handle incoming connections
                Thread th = new Thread(new ThreadStart(StartListen));
                th.Start();
            }
            catch (System.Exception ex)
            {
                // Display an error message if something goes wrong during startup
                Console.WriteLine("Something went wrong:" + ex.Message);
            }
        }

        // Method to handle incoming connections
        private static void StartListen()
        {
            while (true)
            {
                // Accept a new TCP client
                TcpClient client = tcpListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                // Read the incoming request
                byte[] requestBytes = new byte[10240];
                int bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);
                string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = ParseHeaders(request);

                // Extract information from the request
                string[] requestFirstLine = requestHeaders.requestType.Split(' ');
                string httpVersion = requestFirstLine.LastOrDefault();
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept");
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Accept-Encoding");

                if (!request.StartsWith("GET"))
                {
                    // If the request method is not GET, send a 405 Method Not Allowed response
                    SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0, ref stream);
                }
                else
                {
                    // If the request method is GET, attempt to retrieve and send the requested content
                    var requestPath = requestFirstLine[1];
                    var fileContent = GetContent(requestPath);
                    if (fileContent is not null)
                    {
                        // Send a 200 OK response along with the requested content
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, fileContent.Length, ref stream);
                        stream.Write(fileContent, 0, fileContent.Length);
                    }
                    else
                    {
                        // If the requested content is not found, send a 404 Not Found response
                        SendHeaders(httpVersion, 404, "Page Not Found", contentType, contentEncoding, 0, ref stream);
                    }
                }

                // Close the TCP client
                client.Close();
            }
        }

        // Method to retrieve the content of a requested file
        private static byte[] GetContent(string requestedPath)
        {
            if (requestedPath == "/") requestedPath = "index.html";
            string filePath = Path.Join(WebServerPath, requestedPath);

            // Check if the requested file exists
            if (!File.Exists(filePath)) return null;
            else
            {
                // Read the content of the file into a byte array
                byte[] file = File.ReadAllBytes(filePath);
                return file;
            }
        }

        // Method to send HTTP headers in the response
        private static void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding, int byteLength, ref NetworkStream networkStream)
        {
            string responseHeaderBuffer = "";

            // Construct the HTTP response headers
            responseHeaderBuffer = $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                $"Connection: Keep-Alive\r\n" +
                $"Date: {DateTime.UtcNow.ToString()}\r\n" +
                $"Server: Windows PC \r\n" +
                $"Etag: \"{serverEtag}\"r\n" +
                $"Content-Encoding: {contentEncoding}\r\n" +
                "X-Content-Type-Options: nosniff" +
                $"Content-Type: application/signed-exchange;v=b3\r\n\r\n";

            // Convert the headers to a byte array and send them to the client
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeaderBuffer);
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }

        // Method to parse the headers from an HTTP request
        private static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();

            // Iterate over each header line and extract header name-value pairs
            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }

            // Return the parsed headers and the first line of the request
            return (headerValues, firstLine);
        }
    }
}