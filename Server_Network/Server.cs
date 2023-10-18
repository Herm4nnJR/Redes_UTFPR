using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Server_Network
{
    class Server
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 8081;

            TcpListener server = new TcpListener(ip, port);

            try
            {
                server.Start();
                Console.WriteLine($"Server on {ip}:{port}");
                while (true)
                {
                    TcpClient clientSocket = server.AcceptTcpClient();
                    Console.WriteLine($"Accepted connection from {((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port}");
                    Thread clientThread = new Thread(() => HandleClientHttp(clientSocket));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        private static void HandleClientHttp(TcpClient clientSocket)
        {
            try
            {
                NetworkStream networkStream = clientSocket.GetStream();
                networkStream.ReadTimeout = 10000;

                byte[] receiveBuffer = new byte[10240];
                while (true)
                {
                    try
                    {
                        int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                        string incomingMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                        if (bytesRead == 0)
                            break;
                        BrowserAnswer(clientSocket, networkStream, incomingMessage);

                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Erro de leitura: {ex.Message}");
                        break;
                    }
                }
                clientSocket.Close();
                Console.WriteLine("Conexão Encerrada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar: {ex.Message}");
            }
}

        private static void BrowserAnswer(TcpClient clientSocket, NetworkStream networkStream, string incomingMessage)
        {
            Console.WriteLine($"Received: {incomingMessage}");

            if (incomingMessage.Contains("paginahtml"))
            {
                var answer = Encoding.UTF8.GetBytes(
                    "HTTP/1.0 200 OK" + Environment.NewLine
                    + "Content-Length: " + "<h1>Hello World</h1>".Length + Environment.NewLine
                    + "Content-Type: " + "text/html" + Environment.NewLine
                    + "Connection: keep-alive" + Environment.NewLine
                    + Environment.NewLine
                    + "<h1>Hello World</h1>"
                    + Environment.NewLine);
                networkStream.Write(answer, 0, answer.Length);
            }
            else if (incomingMessage.Contains(".jpg"))
            {
                byte[] imageData = File.ReadAllBytes("chuck.jpg"); 

                var answer = Encoding.UTF8.GetBytes(
                    "HTTP/1.0 200 OK" + Environment.NewLine
                    + "Content-Length: " + imageData.Length + Environment.NewLine
                    + "Content-Type: image/jpeg" + Environment.NewLine  
                    + "Connection: keep-alive" + Environment.NewLine
                    + Environment.NewLine);

                networkStream.Write(answer, 0, answer.Length);
                networkStream.Write(imageData, 0, imageData.Length);
            }
            else
            {
                var answer = Encoding.UTF8.GetBytes(
                    $"HTTP/1.0 404 Not Found" + Environment.NewLine
                    + "Content-Length: " + "<h1>error404</h1>".Length + Environment.NewLine
                    + "Content-Type: text/html" + Environment.NewLine
                    + "Connection: close" + Environment.NewLine
                    + Environment.NewLine
                    + "<h1>error404</h1>"
                    + Environment.NewLine);
                networkStream.Write(answer, 0, answer.Length);
            }
        }

        static void HandleClient(TcpClient clientSocket)
        {
            try
            {
                NetworkStream networkStream = clientSocket.GetStream();
                byte[] receiveBuffer = new byte[1024];

                Boolean boolLoop = true;
                while (boolLoop)
                {
                    int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconected");
                        break;
                    }

                    string receivedData = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    Console.WriteLine($"Received: {receivedData}");
                    boolLoop = VerifyContent(clientSocket,networkStream, receivedData);
                }

                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar: {ex.Message}");
            }
        }

        static bool VerifyContent(TcpClient clientSocket, NetworkStream stream, string msg)
        {
            if (msg.ToLower() == "sair")
            {
                return false;
            }
            else if (msg.Length > 9 && msg.Substring(0,9).ToLower() == "arquivo: ")
            {
                string nomeArquivo = msg.Substring(9);
                Console.WriteLine($"Enviando arquivo: {nomeArquivo}");

                using (FileStream fileStream = File.OpenRead(nomeArquivo))
                {
                    using (SHA256 hash = SHA256.Create())
                    {
                        byte[] fileHash = hash.ComputeHash(fileStream);
                        stream.Write(fileHash, 0, fileHash.Length);
                    }
                    fileStream.Seek(0, SeekOrigin.Begin);
                    Thread.Sleep(3000);
                    // Enviar o nome do arquivo
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(nomeArquivo);
                    stream.Write(fileNameBytes, 0, fileNameBytes.Length);

                    // Envia os dados
                    byte[] buffer = new byte[1024];
                    //int bytesRead;
                    //while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    //{
                    Thread.Sleep(3000);
                    int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, bytesRead);
                    //}
                    // Enviar um sinal de término
                    //stream.Write(buffer, 0, 0);
                }

                Console.WriteLine("Arquivo enviado com sucesso!");
                return true;
            }
            else
            {
                byte[] sendData = Encoding.ASCII.GetBytes(msg);
                stream.Write(sendData, 0, sendData.Length);

                return true;
            }
        }
    }
}
