using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Net;
using Microsoft.Data.Sqlite;

namespace Vue_Icons
{
    public partial class Form1 : Form
    {
        private const string AppName = "PS4 App Icon Studio";
        private const string DefaultTitleId = "CUSA00960";
        private const string DefaultFtpPort = "2121";
        private readonly ToolTip toolTip = new();
        private readonly List<Ps4AppEntry> allApps = new();

        private string ConfigPath => Path.Combine(Application.StartupPath, "ip.ini");
        private string AssetRootDirectory => ResolveAssetRootDirectory();
        private string PayloadDirectory => Path.Combine(AssetRootDirectory, "payloads");
        private string ImageDirectory => Path.Combine(AssetRootDirectory, "Img");
        private string GeneratedPayloadDirectory => Path.Combine(Application.StartupPath, "generated");

        private Label headerTitle = null!;
        private Label headerSubtitle = null!;
        private Panel workspacePanel = null!;
        private Panel sidebarPanel = null!;
        private GroupBox groupConnection = null!;
        private GroupBox groupCustom = null!;
        private GroupBox groupApps = null!;
        private GroupBox groupStatus = null!;
        private TextBox textBoxFtpPort = null!;
        private TextBox textBoxAppFilter = null!;
        private TextBox textBoxCustomIconPath = null!;
        private PictureBox pictureBoxCustomPreview = null!;
        private Button buttonBrowseCustomIcon = null!;
        private Button buttonSendCustomIcon = null!;
        private Button buttonLoadApps = null!;
        private Button buttonImportAppDb = null!;
        private ListView listViewApps = null!;
        private RichTextBox richTextBoxStatus = null!;
        private ContextMenuStrip appListMenu = null!;
        private ListView payloadGallery = null!;
        private ImageList payloadGalleryImages = null!;
        private Label labelPayloadCount = null!;
        private Label labelPayloadLibraryValue = null!;
        private Label labelGeneratedValue = null!;
        private Label labelAppLibraryValue = null!;
        private Label labelWorkspaceTarget = null!;
        private Label labelWorkspacePayload = null!;
        private bool isSyncingPayloadSelection;

        private sealed class Ps4AppEntry
        {
            public required string TitleId { get; init; }
            public required string Name { get; init; }
            public string SourceTable { get; init; } = string.Empty;
        }

        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.None;
            InitializeEnhancedUi();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadConnectionSettings();
            LoadAvailablePayloads();
            AppendStatus("Ready. Choose a preset payload or a custom PNG, then send it to the PS4.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveConnectionSettings();
            MessageBox.Show("Connection settings saved.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            AppendStatus("Saved PS4 connection settings.");
        }

        private void button2_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_01", "preset icon 1");
        private void button3_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_02", "preset icon 2");
        private void button4_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_03", "preset icon 3");
        private void button5_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_04", "preset icon 4");
        private void button6_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_05", "preset icon 5");
        private void button7_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_06", "preset icon 6");
        private void button8_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_07", "preset icon 7");
        private void button9_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_08", "preset icon 8");
        private void button10_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_09", "preset icon 9");
        private void button11_Click(object sender, EventArgs e) => SendPresetPayload("preset_icon_10", "preset icon 10");

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                UpdatePayloadPreview(comboBox1.Text);
                SyncGallerySelection(comboBox1.Text);
                AppendStatus($"Selected payload: {comboBox1.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            SendPresetPayload(comboBox1.Text, $"payload {comboBox1.Text}");
        }

        private void InitializeEnhancedUi()
        {
            SuspendLayout();

            Text = AppName;
            BackColor = Color.FromArgb(8, 17, 33);
            ClientSize = new Size(1350, 820);
            MinimumSize = new Size(1350, 820);
            MaximizeBox = true;

            pictureBox1.Visible = false;

            headerTitle = new Label
            {
                AutoSize = true,
                Text = AppName,
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 18)
            };

            headerSubtitle = new Label
            {
                AutoSize = true,
                Text = "Payload browser, icon toolkit, app library tools, and quick console workflow in one place.",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(158, 176, 204),
                Location = new Point(28, 60)
            };

            Controls.Add(headerTitle);
            Controls.Add(headerSubtitle);

            BuildSidebar();
            RestyleConnectionControls();
            BuildWorkspace();
            HideLegacyPresetGrid();
            RestylePayloadBrowser();
            UpdateWorkspaceSummary();

            ResumeLayout(false);
            PerformLayout();
        }

        private void BuildWorkspace()
        {
            workspacePanel = new Panel
            {
                Location = new Point(24, 100),
                Size = new Size(772, 296),
                BackColor = Color.FromArgb(9, 22, 42)
            };
            Controls.Add(workspacePanel);

            Label title = new()
            {
                Text = "Workspace",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };

            Label subtitle = new()
            {
                Text = "A cleaner multi-tool surface for payload browsing, file management, and PS4 app operations.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(158, 176, 204),
                AutoSize = true,
                MaximumSize = new Size(520, 0),
                Location = new Point(22, 48)
            };

            workspacePanel.Controls.Add(title);
            workspacePanel.Controls.Add(subtitle);

            workspacePanel.Controls.Add(CreateMetricCard("Payload Library", "0", new Point(20, 92), out labelPayloadLibraryValue));
            workspacePanel.Controls.Add(CreateMetricCard("Generated Files", "0", new Point(202, 92), out labelGeneratedValue));
            workspacePanel.Controls.Add(CreateMetricCard("App Library", "0", new Point(384, 92), out labelAppLibraryValue));

            Panel targetPanel = new()
            {
                Location = new Point(566, 18),
                Size = new Size(186, 244),
                BackColor = Color.FromArgb(15, 31, 57)
            };

            Label targetTitle = new()
            {
                Text = "Current Target",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 14)
            };

            labelWorkspaceTarget = new Label
            {
                Text = DefaultTitleId,
                ForeColor = Color.FromArgb(101, 194, 255),
                Font = new Font("Consolas", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 48)
            };

            Label payloadLabel = new()
            {
                Text = "Selected Payload",
                ForeColor = Color.FromArgb(158, 176, 204),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(14, 88)
            };

            labelWorkspacePayload = new Label
            {
                Text = "None selected",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                AutoSize = true,
                MaximumSize = new Size(158, 0),
                Location = new Point(14, 110)
            };

            Label targetHint = new()
            {
                Text = "Use the app library on the right to target any installed app, then send a payload or custom icon from the browser below.",
                ForeColor = Color.FromArgb(158, 176, 204),
                AutoSize = true,
                MaximumSize = new Size(156, 0),
                Location = new Point(14, 150)
            };

            targetPanel.Controls.Add(targetTitle);
            targetPanel.Controls.Add(labelWorkspaceTarget);
            targetPanel.Controls.Add(payloadLabel);
            targetPanel.Controls.Add(labelWorkspacePayload);
            targetPanel.Controls.Add(targetHint);
            workspacePanel.Controls.Add(targetPanel);

            FlowLayoutPanel actionPanel = new()
            {
                Location = new Point(20, 182),
                Size = new Size(528, 80),
                BackColor = Color.Transparent,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            actionPanel.Controls.Add(CreateActionButton("Refresh Library", (_, _) => LoadAvailablePayloads(), true));
            actionPanel.Controls.Add(CreateActionButton("Open PayloadKit", (_, _) => OpenFolder(PayloadDirectory), false));
            actionPanel.Controls.Add(CreateActionButton("Open Generated", (_, _) => OpenFolder(GeneratedPayloadDirectory), false));
            actionPanel.Controls.Add(CreateActionButton("Browse Custom PNG", buttonBrowseCustomIcon_Click, false));
            actionPanel.Controls.Add(CreateActionButton("Load PS4 Apps", async (_, _) => await LoadAppsFromPs4Async(), true));
            actionPanel.Controls.Add(CreateActionButton("Import app.db", buttonImportAppDb_Click, false));

            workspacePanel.Controls.Add(actionPanel);
        }

        private Panel CreateMetricCard(string title, string value, Point location, out Label valueLabel)
        {
            Panel card = new()
            {
                Location = location,
                Size = new Size(164, 76),
                BackColor = Color.FromArgb(15, 31, 57)
            };

            Label titleLabel = new()
            {
                Text = title,
                ForeColor = Color.FromArgb(158, 176, 204),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(14, 12)
            };

            valueLabel = new Label
            {
                Text = value,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 28)
            };

            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);
            return card;
        }

        private Button CreateActionButton(string text, EventHandler onClick, bool primary)
        {
            Button button = new()
            {
                Text = text,
                Size = new Size(164, 32),
                Margin = new Padding(0, 0, 12, 10)
            };

            button.Click += onClick;
            StyleButton(button, primary);
            return button;
        }

        private void HideLegacyPresetGrid()
        {
            Control[] legacyControls =
            {
                pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6,
                pictureBox7, pictureBox8, pictureBox9, pictureBox10, pictureBox11,
                button2, button3, button4, button5, button6,
                button7, button8, button9, button10, button11
            };

            foreach (Control control in legacyControls)
            {
                control.Visible = false;
                control.Enabled = false;
            }
        }

        private void BuildSidebar()
        {
            sidebarPanel = new Panel
            {
                BackColor = Color.FromArgb(12, 26, 48),
                Location = new Point(820, 16),
                Size = new Size(500, 770)
            };
            Controls.Add(sidebarPanel);

            groupConnection = new GroupBox
            {
                Text = "PS4 Connection",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(18, 16),
                Size = new Size(464, 164)
            };

            Label labelFtpPort = new()
            {
                Text = "FTP Port",
                ForeColor = Color.FromArgb(201, 214, 234),
                Location = new Point(18, 113),
                AutoSize = true
            };

            textBoxFtpPort = new TextBox
            {
                BackColor = Color.FromArgb(21, 43, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(92, 110),
                Size = new Size(80, 25),
                Text = DefaultFtpPort
            };

            buttonLoadApps = new Button
            {
                Text = "Load Apps From PS4",
                Location = new Point(188, 109),
                Size = new Size(160, 28)
            };
            buttonLoadApps.Click += async (_, _) => await LoadAppsFromPs4Async();

            buttonImportAppDb = new Button
            {
                Text = "Import app.db",
                Location = new Point(354, 109),
                Size = new Size(92, 28)
            };
            buttonImportAppDb.Click += buttonImportAppDb_Click;

            groupConnection.Controls.Add(labelFtpPort);
            groupConnection.Controls.Add(textBoxFtpPort);
            groupConnection.Controls.Add(buttonLoadApps);
            groupConnection.Controls.Add(buttonImportAppDb);
            sidebarPanel.Controls.Add(groupConnection);

            groupCustom = new GroupBox
            {
                Text = "Custom Icon",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(18, 188),
                Size = new Size(464, 220)
            };

            Label labelCustom = new()
            {
                Text = "PNG File",
                ForeColor = Color.FromArgb(201, 214, 234),
                AutoSize = true,
                Location = new Point(18, 36)
            };

            textBoxCustomIconPath = new TextBox
            {
                BackColor = Color.FromArgb(21, 43, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(18, 58),
                Size = new Size(328, 25)
            };

            buttonBrowseCustomIcon = new Button
            {
                Text = "Browse",
                Location = new Point(354, 56),
                Size = new Size(92, 28)
            };
            buttonBrowseCustomIcon.Click += buttonBrowseCustomIcon_Click;

            pictureBoxCustomPreview = new PictureBox
            {
                BackColor = Color.FromArgb(16, 31, 57),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(18, 97),
                Size = new Size(128, 128),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            buttonSendCustomIcon = new Button
            {
                Text = "Send Custom Icon",
                Location = new Point(162, 192),
                Size = new Size(140, 30)
            };
            buttonSendCustomIcon.Click += buttonSendCustomIcon_Click;

            Label labelCustomHint = new()
            {
                Text = "Custom mode uses the largest payload template in this repo and patches your title ID into it.",
                ForeColor = Color.FromArgb(158, 176, 204),
                MaximumSize = new Size(280, 0),
                AutoSize = true,
                Location = new Point(162, 101)
            };

            groupCustom.Controls.Add(labelCustom);
            groupCustom.Controls.Add(textBoxCustomIconPath);
            groupCustom.Controls.Add(buttonBrowseCustomIcon);
            groupCustom.Controls.Add(pictureBoxCustomPreview);
            groupCustom.Controls.Add(buttonSendCustomIcon);
            groupCustom.Controls.Add(labelCustomHint);
            sidebarPanel.Controls.Add(groupCustom);

            groupApps = new GroupBox
            {
                Text = "PS4 App Library",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(18, 416),
                Size = new Size(464, 196)
            };

            textBoxAppFilter = new TextBox
            {
                BackColor = Color.FromArgb(21, 43, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(18, 30),
                Size = new Size(428, 25),
                PlaceholderText = "Filter apps by name or title ID"
            };
            textBoxAppFilter.TextChanged += (_, _) => RefreshAppListView();

            listViewApps = new ListView
            {
                Location = new Point(18, 62),
                Size = new Size(428, 118),
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                View = View.Details,
                BackColor = Color.FromArgb(16, 31, 57),
                ForeColor = Color.White
            };
            listViewApps.Columns.Add("App", 275);
            listViewApps.Columns.Add("Title ID", 120);
            listViewApps.SelectedIndexChanged += listViewApps_SelectedIndexChanged;
            listViewApps.MouseUp += listViewApps_MouseUp;

            appListMenu = new ContextMenuStrip();
            appListMenu.Items.Add("Download Current Icon", null, async (_, _) => await DownloadSelectedAppIconAsync());
            appListMenu.Items.Add("Upload Edited Icon", null, async (_, _) => await UploadSelectedAppIconAsync());
            appListMenu.Items.Add("Use App As Target", null, (_, _) => ApplySelectedAppToTarget());

            groupApps.Controls.Add(textBoxAppFilter);
            groupApps.Controls.Add(listViewApps);
            sidebarPanel.Controls.Add(groupApps);

            groupStatus = new GroupBox
            {
                Text = "Status",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(18, 620),
                Size = new Size(464, 132)
            };

            richTextBoxStatus = new RichTextBox
            {
                Location = new Point(18, 28),
                Size = new Size(428, 88),
                BackColor = Color.FromArgb(16, 31, 57),
                ForeColor = Color.FromArgb(214, 224, 239),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            groupStatus.Controls.Add(richTextBoxStatus);
            sidebarPanel.Controls.Add(groupStatus);

            StyleSidebarButtons();
        }

        private void RestyleConnectionControls()
        {
            textBoxIP.Parent = groupConnection;
            textBoxPort.Parent = groupConnection;
            textBoxTitleId.Parent = groupConnection;
            label1.Parent = groupConnection;
            label2.Parent = groupConnection;
            label3.Parent = groupConnection;
            button1.Parent = groupConnection;

            label1.Text = "IP";
            label2.Text = "Payload Port";
            label3.Text = "Title ID";

            label1.Location = new Point(18, 34);
            textBoxIP.Location = new Point(92, 31);
            textBoxIP.Size = new Size(120, 25);

            label2.Location = new Point(226, 34);
            textBoxPort.Location = new Point(312, 31);
            textBoxPort.Size = new Size(134, 25);

            label3.Location = new Point(18, 73);
            textBoxTitleId.Location = new Point(92, 70);
            textBoxTitleId.Size = new Size(120, 25);

            button1.Location = new Point(312, 68);
            button1.Size = new Size(134, 28);
            button1.Text = "Save Settings";

            StyleTextBox(textBoxIP);
            StyleTextBox(textBoxPort);
            StyleTextBox(textBoxTitleId);
            StyleTextBox(textBoxFtpPort);
            StyleButton(button1, true);
            textBoxTitleId.TextChanged += (_, _) => UpdateWorkspaceSummary();

            label1.ForeColor = Color.FromArgb(201, 214, 234);
            label2.ForeColor = Color.FromArgb(201, 214, 234);
            label3.ForeColor = Color.FromArgb(201, 214, 234);

            toolTip.SetToolTip(textBoxPort, "Payload listener port on the PS4.");
            toolTip.SetToolTip(textBoxFtpPort, "FTP port. Most PS4 FTP payloads use 2121.");
            toolTip.SetToolTip(textBoxTitleId, "Target title ID, for example CUSA00960.");
        }

        private void RestylePresetGrid()
        {
            PictureBox[] presetPictures =
            {
                pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6,
                pictureBox7, pictureBox8, pictureBox9, pictureBox10, pictureBox11
            };

            Button[] presetButtons =
            {
                button2, button3, button4, button5, button6,
                button7, button8, button9, button10, button11
            };

            int startX = 24;
            int startY = 118;
            int cardWidth = 148;
            int cardHeight = 132;
            int gap = 12;

            for (int i = 0; i < presetPictures.Length; i++)
            {
                int row = i / 5;
                int col = i % 5;
                int cardX = startX + (cardWidth + gap) * col;
                int cardY = startY + (cardHeight + gap) * row;

                presetPictures[i].Location = new Point(cardX + 32, cardY + 12);
                presetPictures[i].Size = new Size(84, 84);
                presetPictures[i].SizeMode = PictureBoxSizeMode.StretchImage;
                presetPictures[i].Enabled = true;
                presetPictures[i].BackColor = Color.FromArgb(16, 31, 57);
                presetPictures[i].BorderStyle = BorderStyle.FixedSingle;

                presetButtons[i].Location = new Point(cardX + 22, cardY + 102);
                presetButtons[i].Size = new Size(104, 28);
                presetButtons[i].Text = $"Preset {i + 1}";
                StyleButton(presetButtons[i], false);
            }
        }

        private void RestylePayloadBrowser()
        {
            groupBox1.Text = "Payload Browser";
            groupBox1.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            groupBox1.ForeColor = Color.White;
            groupBox1.BackColor = Color.FromArgb(12, 26, 48);
            groupBox1.Location = new Point(24, 412);
            groupBox1.Size = new Size(772, 340);

            comboBox1.Location = new Point(22, 34);
            comboBox1.Size = new Size(220, 28);
            comboBox1.DropDownHeight = 200;
            comboBox1.FlatStyle = FlatStyle.Flat;
            comboBox1.BackColor = Color.FromArgb(21, 43, 74);
            comboBox1.ForeColor = Color.White;

            labelPayloadCount = new Label
            {
                Location = new Point(258, 39),
                AutoSize = true,
                ForeColor = Color.FromArgb(158, 176, 204),
                BackColor = Color.Transparent,
                Text = "0 payloads"
            };

            pictureBox12.Location = new Point(22, 78);
            pictureBox12.Size = new Size(150, 150);
            pictureBox12.Enabled = true;
            pictureBox12.BackColor = Color.FromArgb(16, 31, 57);
            pictureBox12.BorderStyle = BorderStyle.FixedSingle;
            pictureBox12.SizeMode = PictureBoxSizeMode.Zoom;

            button12.Location = new Point(22, 242);
            button12.Size = new Size(150, 32);
            button12.Text = "Send Selected Payload";
            StyleButton(button12, true);

            Label labelPayloadHint = new()
            {
                Text = "This list reads every .elf in PayloadKit\\payloads. Presets still work, but this browser lets you send any payload file in the folder.",
                Location = new Point(192, 84),
                MaximumSize = new Size(530, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(158, 176, 204),
                BackColor = Color.Transparent
            };

            Label labelPresetHelp = new()
            {
                Text = "Preset payloads patch the title ID automatically before launch. Custom icons use the same send path.",
                Location = new Point(192, 132),
                MaximumSize = new Size(530, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(158, 176, 204),
                BackColor = Color.Transparent
            };

            payloadGalleryImages = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(72, 72)
            };

            payloadGallery = new ListView
            {
                Location = new Point(192, 170),
                Size = new Size(548, 146),
                View = View.LargeIcon,
                MultiSelect = false,
                HideSelection = false,
                BackColor = Color.FromArgb(16, 31, 57),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                LargeImageList = payloadGalleryImages
            };
            payloadGallery.SelectedIndexChanged += payloadGallery_SelectedIndexChanged;
            payloadGallery.DoubleClick += payloadGallery_DoubleClick;

            groupBox1.Controls.Add(labelPayloadHint);
            groupBox1.Controls.Add(labelPresetHelp);
            groupBox1.Controls.Add(labelPayloadCount);
            groupBox1.Controls.Add(payloadGallery);
        }

        private void StyleSidebarButtons()
        {
            StyleButton(buttonLoadApps, true);
            StyleButton(buttonImportAppDb, false);
            StyleButton(buttonBrowseCustomIcon, false);
            StyleButton(buttonSendCustomIcon, true);
        }

        private void OpenFolder(string path)
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = Color.FromArgb(21, 43, 74);
            textBox.ForeColor = Color.White;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = primary ? Color.FromArgb(5, 14, 30) : Color.White;
            button.BackColor = primary ? Color.FromArgb(101, 194, 255) : Color.FromArgb(32, 62, 104);
            button.Cursor = Cursors.Hand;
        }

        private void LoadConnectionSettings()
        {
            if (File.Exists(ConfigPath))
            {
                string[] lines = File.ReadAllLines(ConfigPath);

                if (lines.Length > 0)
                {
                    textBoxIP.Text = lines[0];
                }

                if (lines.Length > 1)
                {
                    textBoxPort.Text = lines[1];
                }

                if (lines.Length > 2)
                {
                    textBoxTitleId.Text = lines[2];
                }

                if (lines.Length > 3)
                {
                    textBoxFtpPort.Text = lines[3];
                }
            }

            if (string.IsNullOrWhiteSpace(textBoxTitleId.Text))
            {
                textBoxTitleId.Text = DefaultTitleId;
            }

            if (string.IsNullOrWhiteSpace(textBoxFtpPort.Text))
            {
                textBoxFtpPort.Text = DefaultFtpPort;
            }
        }

        private void SaveConnectionSettings()
        {
            File.WriteAllLines(ConfigPath, new[]
            {
                textBoxIP.Text,
                textBoxPort.Text,
                textBoxTitleId.Text,
                textBoxFtpPort.Text
            });
        }

        private void LoadAvailablePayloads()
        {
            comboBox1.Items.Clear();
            payloadGallery?.Items.Clear();
            payloadGalleryImages?.Images.Clear();

            if (!Directory.Exists(PayloadDirectory))
            {
                AppendStatus("Payload directory was not found.");
                return;
            }

            string[] payloadNames = Directory
                .GetFiles(PayloadDirectory, "*.elf")
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(GetPayloadSortKey, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            comboBox1.Items.AddRange(payloadNames.Cast<object>().ToArray());
            PopulatePayloadGallery(payloadNames);
            if (labelPayloadCount is not null)
            {
                int presetCount = payloadNames.Count(name => name.StartsWith("preset_", StringComparison.OrdinalIgnoreCase));
                labelPayloadCount.Text = $"{payloadNames.Length} payloads, {presetCount} quick presets";
            }
            UpdateWorkspaceSummary();

            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        private void UpdatePayloadPreview(string payloadName)
        {
            if (string.IsNullOrWhiteSpace(payloadName))
            {
                return;
            }

            string pngPath = Path.Combine(ImageDirectory, payloadName + ".png");
            if (!File.Exists(pngPath))
            {
                return;
            }

            pictureBox12.Image?.Dispose();
            pictureBox12.Image = LoadImageCopy(pngPath);
            UpdateWorkspaceSummary();
        }

        private void PopulatePayloadGallery(IEnumerable<string> payloadNames)
        {
            if (payloadGallery is null || payloadGalleryImages is null)
            {
                return;
            }

            foreach (string payloadName in payloadNames)
            {
                string pngPath = Path.Combine(ImageDirectory, payloadName + ".png");
                Image preview = File.Exists(pngPath)
                    ? CreateGalleryThumbnail(pngPath)
                    : new Bitmap(72, 72);

                payloadGalleryImages.Images.Add(payloadName, preview);

                ListViewItem item = new(GetPayloadDisplayName(payloadName))
                {
                    Name = payloadName,
                    Tag = payloadName,
                    ImageKey = payloadName
                };

                payloadGallery.Items.Add(item);
            }
        }

        private static string GetPayloadSortKey(string payloadName)
        {
            bool isPreset = payloadName.StartsWith("preset_", StringComparison.OrdinalIgnoreCase);
            return (isPreset ? "0_" : "1_") + payloadName;
        }

        private static string GetPayloadDisplayName(string payloadName)
        {
            if (payloadName.StartsWith("preset_icon_", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = payloadName["preset_icon_".Length..];
                return $"Preset {suffix}";
            }

            return payloadName.Replace('_', ' ');
        }

        private static Image CreateGalleryThumbnail(string path)
        {
            using Image source = LoadImageCopy(path);
            Bitmap thumbnail = new(72, 72);
            using Graphics graphics = Graphics.FromImage(thumbnail);
            graphics.Clear(Color.FromArgb(16, 31, 57));
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle destination = FitImage(source.Size, thumbnail.Size);
            graphics.DrawImage(source, destination);
            return thumbnail;
        }

        private void payloadGallery_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isSyncingPayloadSelection || payloadGallery.SelectedItems.Count == 0)
            {
                return;
            }

            string payloadName = payloadGallery.SelectedItems[0].Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(payloadName))
            {
                return;
            }

            isSyncingPayloadSelection = true;
            comboBox1.SelectedItem = payloadName;
            isSyncingPayloadSelection = false;
        }

        private void payloadGallery_DoubleClick(object? sender, EventArgs e)
        {
            if (payloadGallery.SelectedItems.Count == 0)
            {
                return;
            }

            string payloadName = payloadGallery.SelectedItems[0].Tag?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(payloadName))
            {
                SendPresetPayload(payloadName, $"payload {payloadName}");
            }
        }

        private void SyncGallerySelection(string payloadName)
        {
            if (payloadGallery is null || isSyncingPayloadSelection || string.IsNullOrWhiteSpace(payloadName))
            {
                return;
            }

            if (!payloadGallery.Items.ContainsKey(payloadName))
            {
                return;
            }

            isSyncingPayloadSelection = true;
            foreach (ListViewItem existingItem in payloadGallery.Items)
            {
                existingItem.Selected = false;
            }
            ListViewItem? item = payloadGallery.Items[payloadName];
            if (item is null)
            {
                isSyncingPayloadSelection = false;
                return;
            }
            item.Selected = true;
            item.Focused = true;
            item.EnsureVisible();
            isSyncingPayloadSelection = false;
        }

        private void SendPresetPayload(string payloadName, string friendlyName)
        {
            SendPayload(payloadName, friendlyName, null);
        }

        private void SendPayload(string payloadName, string friendlyName, byte[]? customPng)
        {
            if (string.IsNullOrWhiteSpace(textBoxIP.Text) || string.IsNullOrWhiteSpace(textBoxPort.Text))
            {
                MessageBox.Show("Enter the PS4 IP and payload port first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(payloadName))
            {
                MessageBox.Show("Select a payload first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? titleId = NormalizeTitleId(textBoxTitleId.Text);
            if (titleId is null)
            {
                MessageBox.Show("Enter a valid PS4 title ID, for example CUSA00960.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? payloadPath = BuildPayload(payloadName, titleId, customPng);
            if (payloadPath is null)
            {
                return;
            }

            string batchPath = Path.Combine(Application.StartupPath, "send-payload.bat");
            string batchContents =
                "@echo off" + Environment.NewLine +
                $"echo Sending {friendlyName} to {titleId} on {textBoxIP.Text}:{textBoxPort.Text}" + Environment.NewLine +
                $"\"{Path.Combine(AssetRootDirectory, "socat", "socat.exe")}\" -t 99999999 - TCP:{textBoxIP.Text}:{textBoxPort.Text} < \"{payloadPath}\"" + Environment.NewLine +
                "echo Done. If the PS4 icon did not update immediately, back out and refresh the home screen." + Environment.NewLine +
                "pause";

            File.WriteAllText(batchPath, batchContents);

            AppendStatus($"Preparing {friendlyName} for {titleId}.");

            MessageBox.Show(
                $"Sending {friendlyName} to {titleId} on the PS4 now.",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                WorkingDirectory = Application.StartupPath,
                UseShellExecute = true
            });

            AppendStatus($"Sent {friendlyName} to {titleId}.");
            UpdateWorkspaceSummary();
        }

        private string? NormalizeTitleId(string? titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId))
            {
                return null;
            }

            string normalized = titleId.Trim().ToUpperInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[A-Z]{4}[0-9]{5}$")
                ? normalized
                : null;
        }

        private string? BuildPayload(string payloadName, string titleId, byte[]? replacementPng)
        {
            string sourcePayloadPath = Path.Combine(PayloadDirectory, payloadName + ".elf");
            if (!File.Exists(sourcePayloadPath))
            {
                MessageBox.Show("The payload does not exist: " + sourcePayloadPath, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            byte[] payloadBytes = File.ReadAllBytes(sourcePayloadPath);
            byte[] sourceTitleIdBytes = System.Text.Encoding.ASCII.GetBytes(DefaultTitleId);
            byte[] targetTitleIdBytes = System.Text.Encoding.ASCII.GetBytes(titleId);

            int titleIdOffset = FindBytes(payloadBytes, sourceTitleIdBytes);
            if (titleIdOffset < 0)
            {
                MessageBox.Show("The title ID marker was not found in the payload.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Buffer.BlockCopy(targetTitleIdBytes, 0, payloadBytes, titleIdOffset, targetTitleIdBytes.Length);

            if (replacementPng is not null)
            {
                string? pngError = ReplaceEmbeddedPng(payloadBytes, replacementPng);
                if (pngError is not null)
                {
                    MessageBox.Show(pngError, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }

            Directory.CreateDirectory(GeneratedPayloadDirectory);
            string fileSuffix = replacementPng is null ? payloadName : $"{payloadName}_custom";
            string generatedPayloadPath = Path.Combine(GeneratedPayloadDirectory, $"{fileSuffix}_{titleId}.elf");
            File.WriteAllBytes(generatedPayloadPath, payloadBytes);
            return generatedPayloadPath;
        }

        private string? ReplaceEmbeddedPng(byte[] payloadBytes, byte[] replacementPng)
        {
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            int pngStart = FindBytes(payloadBytes, signature);
            if (pngStart < 0)
            {
                return "The PNG block was not found in the payload.";
            }

            int iendOffset = FindBytes(payloadBytes, System.Text.Encoding.ASCII.GetBytes("IEND"), pngStart);
            if (iendOffset < 0)
            {
                return "The PNG end marker was not found in the payload.";
            }

            int pngLength = (iendOffset + 8) - pngStart;
            if (replacementPng.Length > pngLength)
            {
                return $"The custom PNG is too large for this payload template. Limit: {pngLength / 1024} KB, current: {replacementPng.Length / 1024} KB.";
            }

            Array.Clear(payloadBytes, pngStart, pngLength);
            Buffer.BlockCopy(replacementPng, 0, payloadBytes, pngStart, replacementPng.Length);
            return null;
        }

        private int FindBytes(byte[] data, byte[] value, int startIndex = 0)
        {
            for (int i = startIndex; i <= data.Length - value.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < value.Length; j++)
                {
                    if (data[i + j] != value[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        private void buttonBrowseCustomIcon_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new()
            {
                Filter = "PNG files (*.png)|*.png|Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp",
                Title = "Choose a custom icon"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            textBoxCustomIconPath.Text = dialog.FileName;
            pictureBoxCustomPreview.Image?.Dispose();
            pictureBoxCustomPreview.Image = LoadImageCopy(dialog.FileName);
            AppendStatus($"Loaded custom icon: {Path.GetFileName(dialog.FileName)}");
        }

        private void buttonSendCustomIcon_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxCustomIconPath.Text) || !File.Exists(textBoxCustomIconPath.Text))
            {
                MessageBox.Show("Choose a custom image first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            (string? templatePayload, int templateCapacity) = GetLargestPayloadTemplateInfo();
            if (templatePayload is null || templateCapacity <= 0)
            {
                MessageBox.Show("No payload templates were found.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                byte[] customPng = BuildStandardizedIcon(textBoxCustomIconPath.Text, templateCapacity);
                AppendStatus($"Built custom PNG from {Path.GetFileName(textBoxCustomIconPath.Text)} at {customPng.Length / 1024} KB.");
                SendPayload(templatePayload, "custom icon payload", customPng);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to prepare custom icon: {ex.Message}");
                MessageBox.Show(ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private (string? PayloadName, int Capacity) GetLargestPayloadTemplateInfo()
        {
            if (!Directory.Exists(PayloadDirectory))
            {
                return (null, 0);
            }

            string[] payloadPaths = Directory.GetFiles(PayloadDirectory, "*.elf");
            if (payloadPaths.Length == 0)
            {
                return (null, 0);
            }

            string? bestPath = null;
            int bestCapacity = -1;

            foreach (string payloadPath in payloadPaths)
            {
                byte[] bytes = File.ReadAllBytes(payloadPath);
                byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                int start = FindBytes(bytes, signature);
                if (start < 0)
                {
                    continue;
                }

                int iend = FindBytes(bytes, System.Text.Encoding.ASCII.GetBytes("IEND"), start);
                if (iend < 0)
                {
                    continue;
                }

                int length = (iend + 8) - start;
                if (length > bestCapacity)
                {
                    bestCapacity = length;
                    bestPath = payloadPath;
                }
            }

            return bestPath is null
                ? (null, 0)
                : (Path.GetFileNameWithoutExtension(bestPath), bestCapacity);
        }

        private byte[] BuildStandardizedIcon(string sourceImagePath, int? maxBytes = null)
        {
            using Image source = Image.FromFile(sourceImagePath);
            int[] candidateSizes = { 512, 448, 384, 320, 256, 224, 192, 160, 128, 96, 64 };
            byte[]? bestAttempt = null;

            foreach (int iconSize in candidateSizes)
            {
                using Bitmap bitmap = new(iconSize, iconSize);
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                Rectangle destination = FitImage(source.Size, bitmap.Size);
                graphics.DrawImage(source, destination);

                using MemoryStream stream = new();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] pngBytes = stream.ToArray();
                bestAttempt = pngBytes;

                if (maxBytes is null || pngBytes.Length <= maxBytes.Value)
                {
                    return pngBytes;
                }
            }

            throw new InvalidOperationException(
                $"The image is still too large after automatic shrinking. Limit: {maxBytes.GetValueOrDefault() / 1024} KB, smallest attempt: {bestAttempt?.Length / 1024 ?? 0} KB.");
        }

        private static Rectangle FitImage(Size source, Size destination)
        {
            float ratio = Math.Min(destination.Width / (float)source.Width, destination.Height / (float)source.Height);
            int width = Math.Max(1, (int)(source.Width * ratio));
            int height = Math.Max(1, (int)(source.Height * ratio));
            int x = (destination.Width - width) / 2;
            int y = (destination.Height - height) / 2;
            return new Rectangle(x, y, width, height);
        }

        private static Image LoadImageCopy(string path)
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        private async Task LoadAppsFromPs4Async()
        {
            if (string.IsNullOrWhiteSpace(textBoxIP.Text))
            {
                MessageBox.Show("Enter the PS4 IP first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(textBoxFtpPort.Text, out int ftpPort))
            {
                MessageBox.Show("Enter a valid FTP port.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                buttonLoadApps.Enabled = false;
                AppendStatus($"Connecting to PS4 FTP at {textBoxIP.Text}:{ftpPort}.");

                string tempDbPath = Path.Combine(Path.GetTempPath(), $"ps4_app_{Guid.NewGuid():N}.db");
                await Task.Run(() => DownloadAppDbFromPs4(textBoxIP.Text, ftpPort, tempDbPath));
                List<Ps4AppEntry> apps = await Task.Run(() => ReadAppsFromDatabase(tempDbPath));

                allApps.Clear();
                allApps.AddRange(apps.OrderBy(app => app.Name, StringComparer.OrdinalIgnoreCase));
                RefreshAppListView();
                UpdateWorkspaceSummary();

                AppendStatus($"Loaded {allApps.Count} apps from PS4 app.db.");
                MessageBox.Show($"Loaded {allApps.Count} app entries from the PS4.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to load apps from PS4: {ex.Message}");
                MessageBox.Show(
                    "Could not load app.db from the PS4. Make sure an FTP payload is running and the app.db path is accessible.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                buttonLoadApps.Enabled = true;
            }
        }

        private void DownloadAppDbFromPs4(string host, int ftpPort, string destinationPath)
        {
            string[] candidatePaths =
            {
                "/system_data/priv/mms/app.db",
                "/system_data/priv/mms/app.db.bak"
            };

            Exception? lastError = null;

            foreach (string remotePath in candidatePaths)
            {
                try
                {
                    string uri = $"ftp://{host}:{ftpPort}{remotePath}";
                    using FileStream output = File.Create(destinationPath);
                    using Stream input = OpenFtpDownloadStream(uri);
                    input.CopyTo(output);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw new InvalidOperationException("Could not download app.db from the PS4 FTP server.", lastError);
        }

        private Stream OpenFtpDownloadStream(string uri)
        {
#pragma warning disable SYSLIB0014
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential("anonymous", "anonymous");
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            return new ResponseStreamWrapper(response);
        }

        private List<Ps4AppEntry> ReadAppsFromDatabase(string dbPath)
        {
            List<Ps4AppEntry> apps = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            using SqliteConnection connection = new($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();

            List<string> tableNames = new();
            using (SqliteCommand tablesCommand = connection.CreateCommand())
            {
                tablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE 'tbl_appbrowse%';";
                using SqliteDataReader reader = tablesCommand.ExecuteReader();
                while (reader.Read())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }

            foreach (string tableName in tableNames)
            {
                List<string> columns = GetTableColumns(connection, tableName);
                string? titleIdColumn = PickColumn(columns, "titleid", "title_id", "titleId");
                string? titleColumn = PickColumn(columns, "title", "title_name", "titleName", "primary_title");

                if (titleIdColumn is null || titleColumn is null)
                {
                    continue;
                }

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT [{titleIdColumn}], [{titleColumn}] FROM [{tableName}] WHERE [{titleIdColumn}] IS NOT NULL AND [{titleColumn}] IS NOT NULL;";

                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string? titleId = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
                    string? title = reader.IsDBNull(1) ? null : reader.GetString(1)?.Trim();

                    if (string.IsNullOrWhiteSpace(titleId) || string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    string key = $"{titleId}|{title}";
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    apps.Add(new Ps4AppEntry
                    {
                        TitleId = titleId,
                        Name = title,
                        SourceTable = tableName
                    });
                }
            }

            return apps;
        }

        private List<string> GetTableColumns(SqliteConnection connection, string tableName)
        {
            List<string> columns = new();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info([{tableName}]);";

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }

            return columns;
        }

        private string? PickColumn(IEnumerable<string> columns, params string[] names)
        {
            foreach (string name in names)
            {
                string? match = columns.FirstOrDefault(column => string.Equals(column, name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private void buttonImportAppDb_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new()
            {
                Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
                Title = "Select app.db"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                allApps.Clear();
                allApps.AddRange(ReadAppsFromDatabase(dialog.FileName).OrderBy(app => app.Name, StringComparer.OrdinalIgnoreCase));
                RefreshAppListView();
                UpdateWorkspaceSummary();
                AppendStatus($"Imported {allApps.Count} apps from {Path.GetFileName(dialog.FileName)}.");
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to import app.db: {ex.Message}");
                MessageBox.Show("Could not parse the selected app.db file.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshAppListView()
        {
            if (listViewApps is null)
            {
                return;
            }

            listViewApps.BeginUpdate();
            listViewApps.Items.Clear();

            IEnumerable<Ps4AppEntry> apps = allApps;
            string filter = textBoxAppFilter?.Text?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                apps = apps.Where(app =>
                    app.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    app.TitleId.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (Ps4AppEntry app in apps)
            {
                ListViewItem item = new(app.Name);
                item.SubItems.Add(app.TitleId);
                item.Tag = app;
                listViewApps.Items.Add(item);
            }

            listViewApps.EndUpdate();
        }

        private void listViewApps_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listViewApps.SelectedItems.Count == 0)
            {
                return;
            }

            if (listViewApps.SelectedItems[0].Tag is not Ps4AppEntry app)
            {
                return;
            }

            textBoxTitleId.Text = app.TitleId;
            UpdateWorkspaceSummary();
            AppendStatus($"Selected app {app.Name} ({app.TitleId}).");
        }

        private void listViewApps_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            ListViewItem? item = listViewApps.GetItemAt(e.X, e.Y);
            if (item is null)
            {
                return;
            }

            item.Selected = true;
            appListMenu.Show(listViewApps, e.Location);
        }

        private async Task DownloadSelectedAppIconAsync()
        {
            Ps4AppEntry? app = GetSelectedApp();
            if (app is null)
            {
                return;
            }

            if (!TryGetFtpConnection(out string host, out int ftpPort))
            {
                return;
            }

            using SaveFileDialog dialog = new()
            {
                Filter = "PNG files (*.png)|*.png",
                FileName = $"{app.TitleId}_icon0.png",
                Title = $"Save current icon for {app.Name}"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                string remotePath = BuildAppIconRemotePath(app.TitleId);
                AppendStatus($"Downloading current icon for {app.Name} from {remotePath}.");
                await Task.Run(() => DownloadFileFromPs4(host, ftpPort, remotePath, dialog.FileName));
                AppendStatus($"Saved icon for {app.Name} to {dialog.FileName}.");
                MessageBox.Show(
                    $"Downloaded the current icon for {app.Name}.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to download icon for {app.Name}: {ex.Message}");
                MessageBox.Show(
                    "Could not download the current icon from the PS4. The app may use a different path, or FTP may not expose appmeta on this setup.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task UploadSelectedAppIconAsync()
        {
            Ps4AppEntry? app = GetSelectedApp();
            if (app is null)
            {
                return;
            }

            if (!TryGetFtpConnection(out string host, out int ftpPort))
            {
                return;
            }

            using OpenFileDialog dialog = new()
            {
                Filter = "PNG files (*.png)|*.png",
                Title = $"Choose edited icon for {app.Name}"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                byte[] pngBytes = BuildStandardizedIcon(dialog.FileName);
                string remotePath = BuildAppIconRemotePath(app.TitleId);
                AppendStatus($"Uploading edited icon for {app.Name} to {remotePath}.");
                await Task.Run(() => UploadBytesToPs4(host, ftpPort, remotePath, pngBytes));
                textBoxTitleId.Text = app.TitleId;
                textBoxCustomIconPath.Text = dialog.FileName;
                pictureBoxCustomPreview.Image?.Dispose();
                pictureBoxCustomPreview.Image = LoadImageCopy(dialog.FileName);
                AppendStatus($"Uploaded edited icon for {app.Name}.");
                MessageBox.Show(
                    $"Uploaded the edited icon for {app.Name}. Refresh the PS4 home screen if the change does not appear immediately.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to upload icon for {app.Name}: {ex.Message}");
                MessageBox.Show(
                    "Could not upload the edited icon to the PS4. The app may use a different path, or FTP may be read-only on this setup.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ApplySelectedAppToTarget()
        {
            Ps4AppEntry? app = GetSelectedApp();
            if (app is null)
            {
                return;
            }

            textBoxTitleId.Text = app.TitleId;
            UpdateWorkspaceSummary();
            AppendStatus($"Using {app.Name} as the target app.");
        }

        private Ps4AppEntry? GetSelectedApp()
        {
            if (listViewApps.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select an app from the list first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return listViewApps.SelectedItems[0].Tag as Ps4AppEntry;
        }

        private bool TryGetFtpConnection(out string host, out int ftpPort)
        {
            host = textBoxIP.Text.Trim();
            ftpPort = 0;

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Enter the PS4 IP first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(textBoxFtpPort.Text, out ftpPort))
            {
                MessageBox.Show("Enter a valid FTP port.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private string BuildAppIconRemotePath(string titleId)
        {
            return $"/user/appmeta/{titleId}/icon0.png";
        }

        private void DownloadFileFromPs4(string host, int ftpPort, string remotePath, string destinationPath)
        {
            string uri = $"ftp://{host}:{ftpPort}{remotePath}";
            using FileStream output = File.Create(destinationPath);
            using Stream input = OpenFtpDownloadStream(uri);
            input.CopyTo(output);
        }

        private void UploadBytesToPs4(string host, int ftpPort, string remotePath, byte[] data)
        {
            string uri = $"ftp://{host}:{ftpPort}{remotePath}";
#pragma warning disable SYSLIB0014
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential("anonymous", "anonymous");
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.ContentLength = data.Length;

            using Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);

            using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        }

        private string ResolveAssetRootDirectory()
        {
            string preferred = Path.Combine(Application.StartupPath, "PayloadKit");
            if (Directory.Exists(preferred))
            {
                return preferred;
            }

            return Path.Combine(Application.StartupPath, "vue");
        }

        private void UpdateWorkspaceSummary()
        {
            if (labelPayloadLibraryValue is not null)
            {
                int payloadCount = Directory.Exists(PayloadDirectory)
                    ? Directory.GetFiles(PayloadDirectory, "*.elf").Length
                    : 0;
                labelPayloadLibraryValue.Text = payloadCount.ToString();
            }

            if (labelGeneratedValue is not null)
            {
                int generatedCount = Directory.Exists(GeneratedPayloadDirectory)
                    ? Directory.GetFiles(GeneratedPayloadDirectory, "*.elf").Length
                    : 0;
                labelGeneratedValue.Text = generatedCount.ToString();
            }

            if (labelAppLibraryValue is not null)
            {
                labelAppLibraryValue.Text = allApps.Count.ToString();
            }

            if (labelWorkspaceTarget is not null)
            {
                string targetTitle = NormalizeTitleId(textBoxTitleId?.Text) ?? DefaultTitleId;
                labelWorkspaceTarget.Text = targetTitle;
            }

            if (labelWorkspacePayload is not null)
            {
                labelWorkspacePayload.Text = string.IsNullOrWhiteSpace(comboBox1?.Text)
                    ? "None selected"
                    : GetPayloadDisplayName(comboBox1.Text);
            }
        }

        private void AppendStatus(string message)
        {
            if (richTextBoxStatus is null)
            {
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            richTextBoxStatus.AppendText(line + Environment.NewLine);
            richTextBoxStatus.SelectionStart = richTextBoxStatus.TextLength;
            richTextBoxStatus.ScrollToCaret();
        }

        private sealed class ResponseStreamWrapper : Stream
        {
            private readonly FtpWebResponse response;
            private readonly Stream inner;

            public ResponseStreamWrapper(FtpWebResponse response)
            {
                this.response = response;
                inner = response.GetResponseStream() ?? throw new InvalidOperationException("FTP response stream was empty.");
            }

            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => inner.CanSeek;
            public override bool CanWrite => inner.CanWrite;
            public override long Length => inner.Length;
            public override long Position
            {
                get => inner.Position;
                set => inner.Position = value;
            }

            public override void Flush() => inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) => inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                    response.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
