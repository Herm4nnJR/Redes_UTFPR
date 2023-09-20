using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private TcpClient client;
        private NetworkStream stream;
        public Cliente_Form()
        {
            InitializeComponent();
        }
        private void Cliente_Form_Load(object sender, EventArgs e)
        {
            client = new TcpClient("127.0.0.1", 8081); // Endereço IP e porta do servidor
            stream = client.GetStream();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Enviar dados para o servidor quando o botão for clicado
            string mensagem = textBox1.Text;


            byte[] mensagemBytes = Encoding.ASCII.GetBytes(mensagem);
            try
            {
                stream.Write(mensagemBytes, 0, mensagemBytes.Length);

                // Resposta do servidor
                if (mensagem.ToLower() == "sair")
                {
                    this.Close();
                }
                else if (mensagem.Length > 9 && mensagem.Substring(0, 9).ToLower() == "arquivo: ")
                {
                    Thread.Sleep(1000);
                    byte[] receivedHash = new byte[32]; // O hash SHA-256 tem 32 bytes
                    stream.Read(receivedHash, 0, receivedHash.Length);
                    Thread.Sleep(3000);
                    byte[] fileNameBytes = new byte[1024];
                    int bytesRead = stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes, 0, bytesRead);

                    //using (MemoryStream fileData = new MemoryStream())
                    //{
                        MemoryStream fileData = new MemoryStream();
                        byte[] buffer = new byte[1024];
                    Thread.Sleep(3000);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                        fileData.Write(buffer, 0, bytesRead);


                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] calculatedHash = sha256.ComputeHash(fileData.ToArray());

                            if (StructuralComparisons.StructuralEqualityComparer.Equals(calculatedHash, receivedHash))
                            {
                                MessageBox.Show("Hash SHA-256 verificado com sucesso. Os dados do arquivo estão íntegros.");
                            }
                            else
                            {
                                MessageBox.Show("Erro: O hash SHA-256 não corresponde. Os dados do arquivo podem estar corrompidos.");
                            }
                        }

                        File.WriteAllBytes(fileName, fileData.ToArray());
                        MessageBox.Show($"Arquivo salvo com sucesso: {fileName}");
                    //}

                }
                else
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string resposta = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    MessageBox.Show("Resposta: " + resposta);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao enviar: {ex.Message}");
            }

            // Limpa o TextBox
            textBox1.Clear();
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

