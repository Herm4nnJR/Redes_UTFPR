using System;
using System.Collections.Generic;
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

        IPEndPoint endPoint = new IPEndPoint(ip, port);

        UdpClient udpServer = new UdpClient(endPoint);
        //TcpListener server = new TcpListener(ip, port);

        try
        {
            //server.Start();

            while (true)
            {
                List<int> erros = EnviarErrado();

                Console.WriteLine($"Server on {ip}:{port}");

                IPEndPoint clienteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] dadosRecebidos = udpServer.Receive(ref clienteEndPoint);
                string nomeArquivoRecebido = Encoding.UTF8.GetString(dadosRecebidos);

                dadosRecebidos = udpServer.Receive(ref clienteEndPoint);
                int requerido = BitConverter.ToInt32(dadosRecebidos, 0);


                //TcpClient clientSocket = server.AcceptTcpClient();
                //Console.WriteLine($"Accepted connection from {((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port}");

                if (File.Exists(nomeArquivoRecebido))
                    RespostaPositiva(udpServer, clienteEndPoint, nomeArquivoRecebido, erros, requerido);
                else
                {
                    string respNeg = $"O arquivo {nomeArquivoRecebido} não existe.";
                    Console.WriteLine(respNeg);
                    byte[] dadosResposta = BitConverter.GetBytes(255);
                    udpServer.Send(dadosResposta, dadosResposta.Length, clienteEndPoint);
                }
                //Thread clientThread = new Thread(() => HandleClientHttp(clientSocket));
                //clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            //udpServer.Close();
        }
    }

    private static List<int> EnviarErrado()
    {
        List<int> errosEnviados = new List<int>();
        Console.Write("Digite quantidade de erros no envio (se não quiser erros digite 0): ");
        int errosQuant = int.Parse(Console.ReadLine());
        if (errosQuant <= 0)
            Console.WriteLine("Servidor inicializara sem erros");
        else
        {
            for (int i = 0; i < errosQuant; i++)
            {
                Console.Write("Digite quais dos envios devem ser ignorados:");
                errosEnviados.Add(int.Parse(Console.ReadLine()));
            }
            Console.WriteLine("Servidor inicializara com erros");
        }
        return errosEnviados;
    }

    private static void RespostaPositiva(UdpClient udpServer, IPEndPoint clienteEndPoint, string arquivo, List<int> erros, int requerido)
    {
        byte[] respostaTotal = File.ReadAllBytes(arquivo);
        int quantBytes = 60, checksumBytes = 2, indiceBytes = 2;
        ushort indice = 0;
        for (int inicioAtual = 0; inicioAtual < respostaTotal.Length; inicioAtual += quantBytes)
        {
            int comprimento;
            if (quantBytes < respostaTotal.Length - inicioAtual)
                comprimento = quantBytes;
            else
                comprimento = respostaTotal.Length - inicioAtual;
            byte[] parte = new byte[comprimento];

            Array.Copy(respostaTotal, inicioAtual, parte, 0, comprimento);
            byte[] indiceConvertido = BitConverter.GetBytes(indice);
            ushort checksum = GerarCheckSum(parte);
            byte[] checkBytes = BitConverter.GetBytes(checksum);
            byte[] parteComCheck = new byte[comprimento + checksumBytes + indiceBytes];
            Array.Copy(indiceConvertido, parteComCheck, indiceBytes);
            Array.Copy(checkBytes, 0, parteComCheck, checksumBytes, checksumBytes);
            Array.Copy(parte, 0, parteComCheck, (checksumBytes + indiceBytes), comprimento);
            if (!erros.Contains(indice))
            {
                if (requerido == 255 || requerido == indice)
                    udpServer.Send(parteComCheck, parteComCheck.Length, clienteEndPoint);
            }

            indice++;
        }
        Console.WriteLine("Arquivo enviado com sucesso");
    }

    private static ushort GerarCheckSum(byte[] parte)
    {
        ushort check = 0;
        int indexEnd = parte.Length - 1;

        for (int i = 0; i < indexEnd; i += 2)
        {
            check += BitConverter.ToUInt16(parte, i);
        }
        if (check > ushort.MaxValue)
        {
            uint high = (uint)check >> 16;
            check += (ushort)high;
        }
        return check;
    }
    /*       private static void HandleClientHttp(TcpClient clientSocket)
   {
       try
      {
           NetworkStream networkStream = clientSocket.GetStream();
           networkStream.ReadTimeout = 100000;

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
               + "Content-Length: " + "<h1>Trabalhinho Redes</h1>".Length + Environment.NewLine
               + "Content-Type: " + "text/html" + Environment.NewLine
               + "Connection: keep-alive" + Environment.NewLine
               + Environment.NewLine
               + "<h1>Trabalhinho Redes</h1>"
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
   }*/
}
}

//{
//    class Server
//    {
//        static void Main(string[] args)
//        {
//            IPAddress ip = IPAddress.Parse("127.0.0.1");
//            int port = 8081;

//            TcpListener server = new TcpListener(ip, port);

//            try
//            {
//                server.Start();
//                Console.WriteLine($"Server on {ip}:{port}");
//                while (true)
//                {
//                    TcpClient clientSocket = server.AcceptTcpClient();
//                    Console.WriteLine($"Accepted connection from {((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port}");
//                    Thread clientThread = new Thread(() => HandleClientHttp(clientSocket));
//                    clientThread.Start();
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            finally
//            {
//                server.Stop();
//            }
//        }

//        private static void HandleClientHttp(TcpClient clientSocket)
//        {
//            try
//            {
//                NetworkStream networkStream = clientSocket.GetStream();
//                networkStream.ReadTimeout = 10000;

//                byte[] receiveBuffer = new byte[10240];
//                while (true)
//                {
//                    try
//                    {
//                        int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
//                        string incomingMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
//                        if (bytesRead == 0)
//                            break;
//                        BrowserAnswer(clientSocket, networkStream, incomingMessage);

//                    }
//                    catch (IOException ex)
//                    {
//                        Console.WriteLine($"Erro de leitura: {ex.Message}");
//                        break;
//                    }
//                }
//                clientSocket.Close();
//                Console.WriteLine("Conexão Encerrada");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erro ao enviar: {ex.Message}");
//            }
//}

//        private static void BrowserAnswer(TcpClient clientSocket, NetworkStream networkStream, string incomingMessage)
//        {
//            Console.WriteLine($"Received: {incomingMessage}");

//            if (incomingMessage.Contains("paginahtml"))
//            {
//                var answer = Encoding.UTF8.GetBytes(
//                    "HTTP/1.0 200 OK" + Environment.NewLine
//                    + "Content-Length: " + "<h1>Hello World</h1>".Length + Environment.NewLine
//                    + "Content-Type: " + "text/html" + Environment.NewLine
//                    + "Connection: keep-alive" + Environment.NewLine
//                    + Environment.NewLine
//                    + "<h1>Hello World</h1>"
//                    + Environment.NewLine);
//                networkStream.Write(answer, 0, answer.Length);
//            }
//            else if (incomingMessage.Contains(".jpg"))
//            {
//                byte[] imageData = File.ReadAllBytes("chuck.jpg"); 

//                var answer = Encoding.UTF8.GetBytes(
//                    "HTTP/1.0 200 OK" + Environment.NewLine
//                    + "Content-Length: " + imageData.Length + Environment.NewLine
//                    + "Content-Type: image/jpeg" + Environment.NewLine  
//                    + "Connection: keep-alive" + Environment.NewLine
//                    + Environment.NewLine);

//                networkStream.Write(answer, 0, answer.Length);
//                networkStream.Write(imageData, 0, imageData.Length);
//            }
//            else
//            {
//                var answer = Encoding.UTF8.GetBytes(
//                    $"HTTP/1.0 404 Not Found" + Environment.NewLine
//                    + "Content-Length: " + "<h1>error404</h1>".Length + Environment.NewLine
//                    + "Content-Type: text/html" + Environment.NewLine
//                    + "Connection: close" + Environment.NewLine
//                    + Environment.NewLine
//                    + "<h1>error404</h1>"
//                    + Environment.NewLine);
//                networkStream.Write(answer, 0, answer.Length);
//            }
//        }

//        static void HandleClient(TcpClient clientSocket)
//        {
//            try
//            {
//                NetworkStream networkStream = clientSocket.GetStream();
//                byte[] receiveBuffer = new byte[1024];

//                Boolean boolLoop = true;
//                while (boolLoop)
//                {
//                    int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
//                    if (bytesRead == 0)
//                    {
//                        Console.WriteLine("Client disconected");
//                        break;
//                    }

//                    string receivedData = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
//                    Console.WriteLine($"Received: {receivedData}");
//                    boolLoop = VerifyContent(clientSocket,networkStream, receivedData);
//                }

//                clientSocket.Close();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erro ao enviar: {ex.Message}");
//            }
//        }

//        static bool VerifyContent(TcpClient clientSocket, NetworkStream stream, string msg)
//        {
//            if (msg.ToLower() == "sair")
//            {
//                return false;
//            }
//            else if (msg.Length > 9 && msg.Substring(0,9).ToLower() == "arquivo: ")
//            {
//                string nomeArquivo = msg.Substring(9);
//                Console.WriteLine($"Enviando arquivo: {nomeArquivo}");

//                using (FileStream fileStream = File.OpenRead(nomeArquivo))
//                {
//                    using (SHA256 hash = SHA256.Create())
//                    {
//                        byte[] fileHash = hash.ComputeHash(fileStream);
//                        stream.Write(fileHash, 0, fileHash.Length);
//                    }
//                    fileStream.Seek(0, SeekOrigin.Begin);
//                    Thread.Sleep(3000);
//                    // Enviar o nome do arquivo
//                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(nomeArquivo);
//                    stream.Write(fileNameBytes, 0, fileNameBytes.Length);

//                    // Envia os dados
//                    byte[] buffer = new byte[1024];
//                    //int bytesRead;
//                    //while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
//                    //{
//                    Thread.Sleep(3000);
//                    int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
//                    stream.Write(buffer, 0, bytesRead);
//                    //}
//                    // Enviar um sinal de término
//                    //stream.Write(buffer, 0, 0);
//                }

//                Console.WriteLine("Arquivo enviado com sucesso!");
//                return true;
//            }
//            else
//            {
//                byte[] sendData = Encoding.ASCII.GetBytes(msg);
//                stream.Write(sendData, 0, sendData.Length);

//                return true;
//            }
//        }
//    }
//}
