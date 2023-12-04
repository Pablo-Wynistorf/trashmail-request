using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Trashmail_Request
{
    public partial class MainForm : Form
    {
        private readonly string configFilePath = "config.json";
        private string zoneId;
        private string apiKey;
        private string authEmail;
        private string emailDomain;
        private string forwardAddress;
        private const string apiUrl = "https://api.cloudflare.com/client/v4/zones/{0}/email/routing/rules";
        private Random random = new Random();
        private string trashmailIdentifier = string.Empty;

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    var configValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (configValues != null &&
                        configValues.TryGetValue("zoneId", out zoneId) &&
                        configValues.TryGetValue("apiKey", out apiKey) &&
                        configValues.TryGetValue("authEmail", out authEmail) &&
                        configValues.TryGetValue("emailDomain", out emailDomain) &&
                        configValues.TryGetValue("forwardAddress", out forwardAddress))
                    {
                        // Configuration loaded successfully
                    }
                    else
                    {
                        MessageBox.Show("One or more required values are missing in the configuration file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error parsing the configuration file. Details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Configuration file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private const string chars = "abcdefghijklmnopqrstuvwxyz1234567890";

        private string GenerateRandomString(int length)
        {
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            string randomString = GenerateRandomString(5);
            string mailString = $"{randomString}.mail@{emailDomain}";

            string apiUrlFormatted = string.Format(apiUrl, zoneId);

            SendApiRequest(apiUrlFormatted, mailString);
            emailTextBox.Text = mailString;
            deleteButton.Enabled = false;
            generateButton.Enabled = false;
            deleteButton.BackColor = System.Drawing.Color.White;
        }

        private async void SendApiRequest(string apiUrl, string mailString)
        {
            string requestBody = $@"
            {{
                ""actions"": [
                    {{
                        ""type"": ""forward"",
                        ""value"": [
                            ""{forwardAddress}""
                        ]
                    }}
                ],
                ""enabled"": true,
                ""matchers"": [
                    {{
                        ""field"": ""to"",
                        ""type"": ""literal"",
                        ""value"": ""{mailString}""
                    }}
                ],
                ""name"": ""Create temporary trash email address"",
                ""priority"": 0
            }}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Auth-Email", authEmail);
                client.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);

                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    trashmailIdentifier = ExtractTagValue(responseContent);
                    if (!string.IsNullOrEmpty(trashmailIdentifier))
                    {
                        generateButton.BackColor = System.Drawing.Color.Green;
                        generateButton.Enabled = false;
                        deleteButton.Enabled = true;
                        copyButton.Enabled = true;
                        deleteButton.BackColor = System.Drawing.Color.White;
                    }
                    else
                    {
                        generateButton.Enabled = false;
                    }
                }
                else
                {
                    generateButton.BackColor = System.Drawing.Color.Red;
                    deleteButton.Enabled = false;
                    generateButton.Enabled = true;
                }
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(emailTextBox.Text);
            generateButton.BackColor = System.Drawing.Color.White;
            generateButton.Enabled = false;
            copyButton.BackColor = System.Drawing.Color.Green;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(trashmailIdentifier))
            {
                DeleteApiRequest(trashmailIdentifier);
                emailTextBox.Clear();
                generateButton.BackColor = System.Drawing.Color.White;
                generateButton.Enabled = false;
                deleteButton.Enabled = false;
                copyButton.BackColor = System.Drawing.Color.White;
                copyButton.Enabled = false;
            }
            else
            {
                MessageBox.Show("Trashmail identifier is not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteApiRequest(string trashmail)
        {
            string deleteUrl = $"{apiUrl}/{trashmail}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Auth-Email", authEmail);
                client.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);

                HttpResponseMessage response = client.DeleteAsync(deleteUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    trashmailIdentifier = string.Empty;
                    generateButton.BackColor = SystemColors.Control;
                    deleteButton.Enabled = false;
                    deleteButton.BackColor = System.Drawing.Color.Green;
                    generateButton.Enabled = true;
                }
                else
                {
                    deleteButton.BackColor = System.Drawing.Color.Red;
                }
            }
        }

        private string ExtractTagValue(string responseContent)
        {
            int tagIndex = responseContent.IndexOf("\"tag\":");

            if (tagIndex != -1)
            {
                int tagStartIndex = tagIndex + "\"tag\":\"".Length;
                int tagEndIndex = responseContent.IndexOf("\"", tagStartIndex);

                if (tagEndIndex != -1)
                {
                    return responseContent.Substring(tagStartIndex, tagEndIndex - tagStartIndex);
                }
            }

            return null;
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            this.generateButton = new System.Windows.Forms.Button();
            this.emailTextBox = new System.Windows.Forms.TextBox();
            this.copyButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // generateButton
            // 
            this.generateButton.Location = new System.Drawing.Point(12, 12);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(120, 30);
            this.generateButton.TabIndex = 0;
            this.generateButton.Text = "Generate Email";
            this.generateButton.UseVisualStyleBackColor = true;
            this.generateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            // 
            // emailTextBox
            // 
            this.emailTextBox.Location = new System.Drawing.Point(12, 50);
            this.emailTextBox.Name = "emailTextBox";
            this.emailTextBox.Size = new System.Drawing.Size(250, 20);
            this.emailTextBox.TabIndex = 1;
            this.emailTextBox.Text = "";
            this.emailTextBox.Enabled = false;
            // 
            // copyButton
            // 
            this.copyButton.Location = new System.Drawing.Point(268, 48);
            this.copyButton.Name = "copyButton";
            this.copyButton.Size = new System.Drawing.Size(56, 23);
            this.copyButton.TabIndex = 2;
            this.copyButton.Text = "Copy";
            this.copyButton.UseVisualStyleBackColor = true;
            this.copyButton.Click += new System.EventHandler(this.CopyButton_Click);
            this.copyButton.Enabled = false;
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(138, 12);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(120, 30);
            this.deleteButton.TabIndex = 3;
            this.deleteButton.Text = "Delete Email";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Enabled = false;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 91);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.copyButton);
            this.Controls.Add(this.emailTextBox);
            this.Controls.Add(this.generateButton);
            this.Name = "MainForm";
            this.Text = "Random Email Generator";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.TextBox emailTextBox;
        private System.Windows.Forms.Button copyButton;
        private System.Windows.Forms.Button deleteButton;


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}