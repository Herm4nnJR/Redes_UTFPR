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
                    Thread clientThread = new Thread(() => HandleClient(clientSocket));
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
                    VerifyContent(clientSocket,networkStream, receivedData);
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
