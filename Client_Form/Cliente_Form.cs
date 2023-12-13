using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client_Form
{
    public partial class Cliente_Form : Form
    {
        private UdpClient client;
        //private NetworkStream stream;
        public Cliente_Form()
        {
            InitializeComponent();
        }
        private void Cliente_Form_Load(object sender, EventArgs e)
        {

            //client.Connect("127.0.0.1", 8081); // Endereço IP e porta do servidor
            //stream = client.GetStream();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IPAddress ipServidor = IPAddress.Parse("127.0.0.1");
                int portaServidor = 8081;
            client = new UdpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(ipServidor, portaServidor);
            // Enviar dados para o servidor quando o botão for clicado
            string mensagem = textBox1.Text;


            byte[] mensagemBytes = Encoding.ASCII.GetBytes(mensagem);
            client.Send(mensagemBytes, mensagemBytes.Length, ipServidor.ToString(), portaServidor);
            // caso 255 seja enviado, está requisitando todo o arquivo
            byte[] pedidoInteiro = BitConverter.GetBytes(255);
            client.Send(pedidoInteiro, pedidoInteiro.Length, ipServidor.ToString(), portaServidor);
            try
            {
                using (MemoryStream arquivo = new MemoryStream())
                {
                    int tamanho = 64, ultimoIndice = 0, indiceAtual;
                    byte[] buffer = new byte[tamanho], bufSemInd= new byte[tamanho - 4], arrayArquivos;
                    //int parteAtual = 0;
                    bool checker = true;
                    List<int> indices = new List<int>(), faltantes = new List<int>();

                    while (true)
                    {
                        buffer = client.Receive(ref serverEndPoint);

                        if (buffer.Length == 0)
                        {
                            // Nenhuma parte adicional, sair do loop
                            break;
                        }
                        if (buffer.Length == 4 && buffer.ElementAt(0) == 255)
                        {
                            MessageBox.Show("Arquivo não existe, tente outro arquivo!");
                            checker = false;
                            break;
                        }
                        if (buffer.Length < tamanho)
                            tamanho = buffer.Length;
                        indiceAtual = BitConverter.ToUInt16(buffer, 0);
                        indices.Add(indiceAtual);
                        checker = checkSum(buffer);

                        while (indiceAtual > (ultimoIndice + 1))
                        {
                            byte[] bufferZerado = new byte[60];
                            for (int i = 0; i < bufferZerado.Length; i++)
                            {
                                bufferZerado[i] = 0;
                            }
                            arquivo.Write(bufferZerado, 0, bufferZerado.Length);
                            ultimoIndice++;
                        }
                        Array.Copy(buffer, 4, bufSemInd, 0, tamanho - 4);
                        arquivo.Write(bufSemInd, 0, tamanho - 4);

                        //parteAtual++;
                        //Console.WriteLine($"Recebida parte {parteAtual}");
                        if (tamanho < 64)
                            break;
                        ultimoIndice = indiceAtual;
                    }
                    faltantes = checkIndices(indices);
                    if (faltantes.Count > 0)
                    {
                        MessageBox.Show("Alguma mensagem se perdeu, requerindo os pacotes que se perderam");
                        foreach (int faltante in faltantes)
                        {
                            //excluir

                            //Console.WriteLine("Dados no MemoryStream:");
                            //foreach (byte valor in arquivo.ToArray())
                            //{
                            //    Console.Write(valor + " ");
                            //}
                            ////
                            client.Send(mensagemBytes, mensagemBytes.Length, ipServidor.ToString(), portaServidor);

                            pedidoInteiro = BitConverter.GetBytes(faltante);
                            client.Send(pedidoInteiro, pedidoInteiro.Length, ipServidor.ToString(), portaServidor);
                            buffer = client.Receive(ref serverEndPoint);
                            Array.Copy(buffer, 4, bufSemInd, 0, buffer.Length - 4 );
                            arquivo.Seek(faltante * 60, SeekOrigin.Begin);
                            arquivo.Write(bufSemInd, 0, bufSemInd.Length);

                            ////excluir

                            //Console.WriteLine("Dados no MemoryStream depois:");
                            //foreach (byte valor in arquivo.ToArray())
                            //{
                            //    Console.Write(valor + " ");
                            //}
                            ////
                        }
                    }
                    if (checker)
                    {
                        File.WriteAllBytes($"{mensagem}", arquivo.ToArray());
                        MessageBox.Show($"Arquivo {mensagem} Recebido com sucesso!");
                    }
                    else
                        MessageBox.Show("Ouve erro de comunicação, necessário nova requisição");
                    
                }

                //// Resposta do servidor
                //if (mensagem.ToLower() == "sair")
                //{
                //    this.Close();
                //}
                //else if (mensagem.Length > 9 && mensagem.Substring(0, 9).ToLower() == "arquivo: ")
                //{
                //    Thread.Sleep(1000);
                //    byte[] receivedHash = new byte[32]; // O hash SHA-256 tem 32 bytes
                //    stream.Read(receivedHash, 0, receivedHash.Length);
                //    Thread.Sleep(3000);
                //    byte[] fileNameBytes = new byte[1024];
                //    int bytesRead = stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                //    string fileName = Encoding.UTF8.GetString(fileNameBytes, 0, bytesRead);

                //    //using (MemoryStream fileData = new MemoryStream())
                //    //{
                //        MemoryStream fileData = new MemoryStream();
                //        byte[] buffer = new byte[1024];
                //    Thread.Sleep(3000);
                //    bytesRead = stream.Read(buffer, 0, buffer.Length);

                //        fileData.Write(buffer, 0, bytesRead);


                //        using (SHA256 sha256 = SHA256.Create())
                //        {
                //            byte[] calculatedHash = sha256.ComputeHash(fileData.ToArray());

                //            if (StructuralComparisons.StructuralEqualityComparer.Equals(calculatedHash, receivedHash))
                //            {
                //                MessageBox.Show("Hash SHA-256 verificado com sucesso. Os dados do arquivo estão íntegros.");
                //            }
                //            else
                //            {
                //                MessageBox.Show("Erro: O hash SHA-256 não corresponde. Os dados do arquivo podem estar corrompidos.");
                //            }
                //        }

                //        File.WriteAllBytes(fileName, fileData.ToArray());
                //        MessageBox.Show($"Arquivo salvo com sucesso: {fileName}");
                //    //}

                //}
                //else
                //{
                //    byte[] buffer = new byte[1024];
                //    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                //    string resposta = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                //    MessageBox.Show("Resposta: " + resposta);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
            finally
            {
                client.Close();
            }

            // Limpa o TextBox
            textBox1.Clear();
        }



        private List<int> checkIndices(List<int> indices)
        {
            List<int> retorno = new List<int>();
            int i = 0;
            foreach (int ind in indices)
            {
                while (ind > i)
                {
                    retorno.Add(i);
                    i++;
                }
                i++;
            }
            return retorno;
        }

        private bool checkSum(byte[] buffer)
        {
            ushort check = 0;
            int indexEnd = buffer.Length - 1;

            for (int i = 4; i< indexEnd; i += 2) //inicia no 4 pra nao usar bytes de indice e checksum
            {
                check += BitConverter.ToUInt16(buffer, i);
            }
            if (check > ushort.MaxValue)
            {
                uint high = (uint)check >> 16;
                check += (ushort)high;
            }
            ushort valorCheckSum = BitConverter.ToUInt16(buffer, 2);
            return (check ==valorCheckSum );
        }

        private Button button2;
        private TextBox textBox1;

        private void InitializeComponent()
        {
            this.button2 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(550, 54);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 0;
            this.button2.Text = "Enviar";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(613, 22);
            this.textBox1.TabIndex = 1;
            // 
            // Cliente_Form
            // 
            this.ClientSize = new System.Drawing.Size(637, 253);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button2);
            this.Name = "Cliente_Form";
            this.Text = "Cliente";
            this.Load += new System.EventHandler(this.Cliente_Form_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }


    }
}

