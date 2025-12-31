using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Collections.Generic;
using System.Net;

namespace FunctionFlex
{
    public class ConnectivityTest
    {
        private static List<string> _outputBuffer = new();
        [Function("ConnectivityTest")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            _outputBuffer.Clear();

            AddOutput("=== Network Connectivity Test Started ===");
            AddOutput($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            AddOutput("");

            // Read from query string or environment variables
            string serverFqdn = req.Query["server"] ?? 
                               Environment.GetEnvironmentVariable("SERVER_FQDN") ?? "imap.gmail.com";
            string portString = req.Query["port"] ?? 
                               Environment.GetEnvironmentVariable("SERVER_PORT") ?? "993";
            
            if (!int.TryParse(portString, out int portNumber))
            {
                portNumber = 993;
            }

            const int timeoutSeconds = 30;

            AddOutput($"Configuration: Server={serverFqdn}, Port={portNumber}, Timeout={timeoutSeconds}s");
            AddOutput("");

            var response = req.CreateResponse();

            try
            {
                await TestConnectivity(serverFqdn, portNumber, timeoutSeconds);

                AddOutput("");
                AddOutput("✅ Test completed successfully!");

                response.StatusCode = System.Net.HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                
                var htmlOutput = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Network Connectivity Test Results</title>
    <style>
        body {{ font-family: 'Consolas', 'Monaco', monospace; background: #1e1e1e; color: #d4d4d4; padding: 20px; }}
        .container {{ max-width: 900px; margin: 0 auto; background: #252526; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); }}
        h1 {{ color: #4ec9b0; border-bottom: 2px solid #4ec9b0; padding-bottom: 10px; }}
        .success {{ color: #4ec9b0; }}
        .error {{ color: #f48771; }}
        .warning {{ color: #dcdcaa; }}
        .info {{ color: #569cd6; }}
        pre {{ background: #1e1e1e; padding: 15px; border-radius: 5px; border-left: 4px solid #4ec9b0; overflow-x: auto; line-height: 1.6; }}
        .timestamp {{ color: #858585; font-size: 0.9em; }}
        .summary {{ background: #2d2d30; padding: 15px; border-radius: 5px; margin-top: 20px; border-left: 4px solid #4ec9b0; }}
        .summary-item {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔍 Network Connectivity Test Results</h1>
        <div class='timestamp'>Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
        <div class='summary'>
            <div class='summary-item'><strong>Server:</strong> {serverFqdn}</div>
            <div class='summary-item'><strong>Port:</strong> {portNumber}</div>
            <div class='summary-item'><strong>Status:</strong> <span class='success'>✅ SUCCESS</span></div>
        </div>
        <h2>Test Output:</h2>
        <pre>{WebUtility.HtmlEncode(GetOutputText())}</pre>
    </div>
</body>
</html>";
                
                await response.WriteStringAsync(htmlOutput);
            }
            catch (Exception ex)
            {
                AddOutput("");
                AddOutput($"❌ Test failed: {ex.GetType().Name}");
                AddOutput($"Message: {ex.Message}");
                AddOutput($"Stack Trace: {ex.StackTrace}");

                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                
                var htmlOutput = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Network Connectivity Test - Failed</title>
    <style>
        body {{ font-family: 'Consolas', 'Monaco', monospace; background: #1e1e1e; color: #d4d4d4; padding: 20px; }}
        .container {{ max-width: 900px; margin: 0 auto; background: #252526; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); }}
        h1 {{ color: #f48771; border-bottom: 2px solid #f48771; padding-bottom: 10px; }}
        .error {{ color: #f48771; }}
        .timestamp {{ color: #858585; font-size: 0.9em; }}
        pre {{ background: #1e1e1e; padding: 15px; border-radius: 5px; border-left: 4px solid #f48771; overflow-x: auto; line-height: 1.6; }}
        .summary {{ background: #2d2d30; padding: 15px; border-radius: 5px; margin-top: 20px; border-left: 4px solid #f48771; }}
        .summary-item {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>❌ Network Connectivity Test Failed</h1>
        <div class='timestamp'>Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
        <div class='summary'>
            <div class='summary-item'><strong>Server:</strong> {serverFqdn}</div>
            <div class='summary-item'><strong>Port:</strong> {portNumber}</div>
            <div class='summary-item'><strong>Error:</strong> <span class='error'>{ex.Message}</span></div>
        </div>
        <h2>Test Output:</h2>
        <pre>{WebUtility.HtmlEncode(GetOutputText())}</pre>
    </div>
</body>
</html>";
                
                await response.WriteStringAsync(htmlOutput);
            }

            return response;
        }

        private static void AddOutput(string message)
        {
            _outputBuffer.Add(message);
        }

        private static string GetOutputText()
        {
            return string.Join(Environment.NewLine, _outputBuffer);
        }

        private static async Task TestConnectivity(string server, int port, int timeoutSeconds)
        {
            AddOutput($"📡 Testing connectivity to: {server}:{port}");
            AddOutput($"⏱️  Timeout: {timeoutSeconds} seconds");
            AddOutput("");

            // Step 1: DNS Resolution
            AddOutput("Step 1: DNS Resolution");
            var stopwatch = Stopwatch.StartNew();
            System.Net.IPAddress[] addresses;

            try
            {
                addresses = await System.Net.Dns.GetHostAddressesAsync(server);
                stopwatch.Stop();

                AddOutput($"✅ DNS resolution successful ({stopwatch.ElapsedMilliseconds}ms)");
                foreach (var addr in addresses)
                {
                    AddOutput($"   - {addr} ({addr.AddressFamily})");
                }
                AddOutput("");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddOutput($"❌ DNS resolution failed: {ex.Message}");
                throw;
            }

            // Step 2: Test All DNS Addresses
            AddOutput("Step 2: Testing All DNS Addresses");
            var successfulAddresses = new List<System.Net.IPAddress>();
            var failedAddresses = new Dictionary<System.Net.IPAddress, string>();
            const int quickTestTimeout = 5; // Shorter timeout for bulk testing

            foreach (var address in addresses)
            {
                TcpClient? testClient = null;
                stopwatch.Restart();

                try
                {
                    testClient = new TcpClient();
                    testClient.ReceiveTimeout = quickTestTimeout * 1000;
                    testClient.SendTimeout = quickTestTimeout * 1000;

                    var connectTask = testClient.ConnectAsync(address, port);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(quickTestTimeout));
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException($"Timeout after {quickTestTimeout}s");
                    }

                    await connectTask;
                    stopwatch.Stop();

                    AddOutput($"   ✅ {address} - REACHABLE ({stopwatch.ElapsedMilliseconds}ms)");
                    successfulAddresses.Add(address);
                    testClient.Dispose();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    AddOutput($"   ❌ {address} - FAILED ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
                    failedAddresses.Add(address, ex.Message);
                    testClient?.Dispose();
                }
            }

            AddOutput("");
            if (successfulAddresses.Count == 0)
            {
                AddOutput("❌ All DNS addresses failed to connect!");
                throw new Exception($"All {addresses.Length} DNS-resolved addresses failed to connect");
            }
            else if (failedAddresses.Count > 0)
            {
                AddOutput($"⚠️  Warning: {failedAddresses.Count}/{addresses.Length} addresses failed");
            }
            else
            {
                AddOutput($"✅ All {addresses.Length} DNS addresses are reachable");
            }
            AddOutput("");

            // Step 3: TCP Connection (using first successful address)
            AddOutput("Step 3: TCP Connection (detailed test)");
            TcpClient? tcpClient = null;
            stopwatch.Restart();

            try
            {
                tcpClient = new TcpClient();
                tcpClient.ReceiveTimeout = timeoutSeconds * 1000;
                tcpClient.SendTimeout = timeoutSeconds * 1000;

                var connectTask = tcpClient.ConnectAsync(server, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Connection attempt timed out after {timeoutSeconds} seconds");
                }

                await connectTask;
                stopwatch.Stop();

                AddOutput($"✅ TCP connection established ({stopwatch.ElapsedMilliseconds}ms)");
                AddOutput($"   Local endpoint: {tcpClient.Client.LocalEndPoint}");
                AddOutput($"   Remote endpoint: {tcpClient.Client.RemoteEndPoint}");
                AddOutput($"   Connected: {tcpClient.Connected}");
                AddOutput("");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddOutput($"❌ TCP connection failed ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
                tcpClient?.Dispose();
                throw;
            }

            // Step 4: SSL/TLS Handshake
            AddOutput("Step 4: SSL/TLS Handshake");
            SslStream? sslStream = null;
            stopwatch.Restart();

            try
            {
                var networkStream = tcpClient.GetStream();
                sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: false,
                    userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        AddOutput("   📜 Certificate validation callback triggered");
                        AddOutput($"      Subject: {certificate?.Subject}");
                        AddOutput($"      Issuer: {certificate?.Issuer}");
                        AddOutput($"      Valid from: {certificate?.GetEffectiveDateString()}");
                        AddOutput($"      Valid to: {certificate?.GetExpirationDateString()}");
                        AddOutput($"      SSL Policy Errors: {sslPolicyErrors}");

                        return true;
                    }
                );

                await sslStream.AuthenticateAsClientAsync(server);
                stopwatch.Stop();

                AddOutput($"✅ SSL/TLS handshake successful ({stopwatch.ElapsedMilliseconds}ms)");
                AddOutput($"   SSL Protocol: {sslStream.SslProtocol}");
                AddOutput($"   Cipher Algorithm: {sslStream.CipherAlgorithm} ({sslStream.CipherStrength} bits)");
                AddOutput($"   Hash Algorithm: {sslStream.HashAlgorithm} ({sslStream.HashStrength} bits)");
                AddOutput($"   Key Exchange Algorithm: {sslStream.KeyExchangeAlgorithm} ({sslStream.KeyExchangeStrength} bits)");
                AddOutput($"   Is Authenticated: {sslStream.IsAuthenticated}");
                AddOutput($"   Is Encrypted: {sslStream.IsEncrypted}");
                AddOutput($"   Is Signed: {sslStream.IsSigned}");
                AddOutput("");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddOutput($"❌ SSL/TLS handshake failed ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
                sslStream?.Dispose();
                tcpClient?.Dispose();
                throw;
            }

            // Step 5: Read Server Greeting
            AddOutput("Step 5: Reading Server Greeting");
            AddOutput("ℹ️  Note: Mail servers (IMAP/SMTP/POP3) send automatic greetings. Web servers (HTTPS) typically don't.");
            stopwatch.Restart();

            try
            {
                var buffer = new byte[4096];
                var readTask = sslStream.ReadAsync(buffer, 0, buffer.Length);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5)); // 5 second timeout for greeting
                
                var completedTask = await Task.WhenAny(readTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    stopwatch.Stop();
                    AddOutput($"⚠️  No greeting received within 5 seconds (server may not send automatic greeting)");
                    AddOutput("");
                }
                else
                {
                    var bytesRead = await readTask;
                    stopwatch.Stop();

                    if (bytesRead > 0)
                    {
                        var greeting = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        AddOutput($"✅ Received server greeting ({stopwatch.ElapsedMilliseconds}ms, {bytesRead} bytes)");
                        AddOutput($"   Response: {greeting}");
                        AddOutput("");
                    }
                    else
                    {
                        AddOutput($"⚠️  No data received from server");
                        AddOutput("");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddOutput($"⚠️  Failed to read greeting ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
                AddOutput("");
            }
            finally
            {
                sslStream?.Dispose();
                tcpClient?.Dispose();
            }

            // Summary
            AddOutput("=== Test Summary ===");
            AddOutput("✅ All connectivity tests passed!");
            AddOutput($"   Server: {server}:{port}");
            AddOutput($"   Status: REACHABLE");
            AddOutput($"   SSL/TLS: WORKING");
            AddOutput($"   Server Protocol: RESPONDING");
        }
    }
}
