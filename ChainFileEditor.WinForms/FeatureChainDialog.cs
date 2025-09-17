using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Configuration;

namespace ChainFileEditor.WinForms
{
    public partial class FeatureChainDialog : Form
    {
        private readonly FeatureChainService _featureService;
        private TextBox _jiraIdTextBox;
        private TextBox _descriptionTextBox;
        private TextBox _versionTextBox;
        private TextBox _devsVersionTextBox;
        private TextBox _recipientsTextBox;
        private DataGridView _projectsGrid;
        private ListView _integrationTestsListView;
        private Button _createButton;

        public string CreatedFilePath { get; private set; }

        public FeatureChainDialog(FeatureChainService featureService)
        {
            _featureService = featureService;
            InitializeComponent();
            LoadAvailableProjects();
            LoadIntegrationTests();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Dialog properties
            this.Text = "Create Feature Chain";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Main container
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(20)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Basic info panel
            var infoPanel = CreateBasicInfoPanel();
            mainPanel.Controls.Add(infoPanel, 0, 0);
            mainPanel.SetColumnSpan(infoPanel, 2);

            // Projects panel
            var projectsPanel = CreateProjectsPanel();
            mainPanel.Controls.Add(projectsPanel, 0, 1);
            mainPanel.SetRowSpan(projectsPanel, 2);

            // Integration tests panel
            var testsPanel = CreateIntegrationTestsPanel();
            mainPanel.Controls.Add(testsPanel, 1, 1);

            // Global settings panel
            var globalPanel = CreateGlobalSettingsPanel();
            mainPanel.Controls.Add(globalPanel, 1, 2);

            // Buttons panel
            var buttonsPanel = CreateButtonsPanel();
            mainPanel.Controls.Add(buttonsPanel, 0, 3);
            mainPanel.SetColumnSpan(buttonsPanel, 2);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private Panel CreateBasicInfoPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var titleLabel = new Label
            {
                Text = "Feature Chain Information",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                Size = new Size(300, 25)
            };

            var jiraLabel = new Label
            {
                Text = "JIRA ID:",
                Location = new Point(15, 45),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            _jiraIdTextBox = new TextBox
            {
                Location = new Point(100, 43),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var descLabel = new Label
            {
                Text = "Description:",
                Location = new Point(270, 45),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            _descriptionTextBox = new TextBox
            {
                Location = new Point(360, 43),
                Size = new Size(400, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var versionLabel = new Label
            {
                Text = "Integration Version:",
                Location = new Point(15, 80),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            _versionTextBox = new TextBox
            {
                Location = new Point(140, 78),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "10159"
            };

            var devsVersionLabel = new Label
            {
                Text = "Feature Version:",
                Location = new Point(260, 80),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            _devsVersionTextBox = new TextBox
            {
                Location = new Point(370, 78),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "30588"
            };

            var recipientsLabel = new Label
            {
                Text = "Recipients:",
                Location = new Point(490, 80),
                Size = new Size(70, 20),
                ForeColor = Color.White
            };

            _recipientsTextBox = new TextBox
            {
                Location = new Point(570, 78),
                Size = new Size(190, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "email1@infor.com,email2@infor.com"
            };

            panel.Controls.AddRange(new Control[] {
                titleLabel, jiraLabel, _jiraIdTextBox, descLabel, _descriptionTextBox,
                versionLabel, _versionTextBox, devsVersionLabel, _devsVersionTextBox,
                recipientsLabel, _recipientsTextBox
            });

            return panel;
        }

        private Panel CreateProjectsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var titleLabel = new Label
            {
                Text = "Project Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30
            };

            _projectsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9)
            };

            // Configure grid columns
            var includeCol = new DataGridViewCheckBoxColumn
            {
                Name = "Include",
                HeaderText = "Include",
                Width = 60
            };

            var projectCol = new DataGridViewTextBoxColumn
            {
                Name = "Project",
                HeaderText = "Project",
                ReadOnly = true,
                Width = 120
            };

            var modeCol = new DataGridViewComboBoxColumn
            {
                Name = "Mode",
                HeaderText = "Mode",
                Width = 80
            };
            modeCol.Items.AddRange(new[] { "source", "binary", "ignore" });

            var devModeCol = new DataGridViewComboBoxColumn
            {
                Name = "DevMode",
                HeaderText = "Dev Mode",
                Width = 80
            };
            devModeCol.Items.AddRange(new[] { "", "binary", "ignore" });

            var branchCol = new DataGridViewTextBoxColumn
            {
                Name = "Branch",
                HeaderText = "Branch",
                Width = 150
            };

            var tagCol = new DataGridViewTextBoxColumn
            {
                Name = "Tag",
                HeaderText = "Tag",
                Width = 120
            };

            var forkCol = new DataGridViewComboBoxColumn
            {
                Name = "Fork",
                HeaderText = "Fork",
                Width = 120
            };
            forkCol.Items.Add("");

            _projectsGrid.Columns.AddRange(new DataGridViewColumn[] {
                includeCol, projectCol, modeCol, devModeCol, branchCol, tagCol, forkCol
            });

            // Style the grid
            _projectsGrid.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            _projectsGrid.DefaultCellStyle.ForeColor = Color.White;
            _projectsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            _projectsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            _projectsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _projectsGrid.EnableHeadersVisualStyles = false;

            // Add event handler for updating fork options
            _projectsGrid.CellEnter += ProjectsGrid_CellEnter;

            panel.Controls.Add(_projectsGrid);
            panel.Controls.Add(titleLabel);

            return panel;
        }

        private Panel CreateIntegrationTestsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var titleLabel = new Label
            {
                Text = "Integration Tests",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30
            };

            _integrationTestsListView = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9)
            };

            _integrationTestsListView.Columns.Add("Test Suite", 180);
            _integrationTestsListView.Columns.Add("Category", 100);

            panel.Controls.Add(_integrationTestsListView);
            panel.Controls.Add(titleLabel);

            return panel;
        }

        private Panel CreateGlobalSettingsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var titleLabel = new Label
            {
                Text = "Global Settings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                Size = new Size(200, 25)
            };

            var infoLabel = new Label
            {
                Text = "• Integration Version: Used for upstream projects\n" +
                       "• Feature Version: Used for downstream projects\n" +
                       "• Recipients: Email notifications for build results",
                ForeColor = Color.LightGray,
                Location = new Point(15, 45),
                Size = new Size(300, 80),
                Font = new Font("Segoe UI", 8)
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(infoLabel);

            return panel;
        }

        private Panel CreateButtonsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            _createButton = new Button
            {
                Text = "Create Feature Chain",
                Size = new Size(180, 35),
                Location = new Point(20, 8),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _createButton.Click += CreateButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(220, 8),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            panel.Controls.Add(_createButton);
            panel.Controls.Add(cancelButton);

            return panel;
        }

        private void LoadAvailableProjects()
        {
            var config = ConfigurationLoader.LoadValidationConfig();
            var allProjects = config.AllProjects;

            foreach (var project in allProjects)
            {
                var rowIndex = _projectsGrid.Rows.Add();
                var row = _projectsGrid.Rows[rowIndex];

                row.Cells["Include"].Value = false;
                row.Cells["Project"].Value = project;
                row.Cells["Mode"].Value = "source";
                row.Cells["DevMode"].Value = GetDefaultDevMode(project);
                row.Cells["Branch"].Value = "main";
                row.Cells["Tag"].Value = "";
                row.Cells["Fork"].Value = "";
                
                // Initialize fork options for this project
                UpdateForkOptions(rowIndex, project);
            }
        }

        private void LoadIntegrationTests()
        {
            var config = ConfigurationLoader.LoadValidationConfig();
            var testSuites = config.IntegrationTestSuites;

            var categories = new Dictionary<string, string>
            {
                ["AdministrationService"] = "Admin",
                ["AppEngineService"] = "Engine",
                ["AppStudioService"] = "Studio",
                ["BusinessModelingServiceSet1"] = "Modeling",
                ["BusinessModelingServiceSet2"] = "Modeling",
                ["ConsolidationService"] = "Consolidation",
                ["DashboardsService"] = "Dashboards",
                ["OfficeIntegrationService"] = "Office",
                ["OlapService"] = "OLAP",
                ["OlapAPI"] = "OLAP",
                ["WorkforceBudgetingSet1"] = "Budgeting",
                ["WorkforceBudgetingSet2"] = "Budgeting"
            };

            foreach (var testSuite in testSuites)
            {
                var item = new ListViewItem(testSuite);
                var category = categories.ContainsKey(testSuite) ? categories[testSuite] : "General";
                item.SubItems.Add(category);
                item.Tag = testSuite;
                _integrationTestsListView.Items.Add(item);
            }
        }

        private string GetDefaultDevMode(string project)
        {
            return project switch
            {
                "designer" => "ignore",
                "deployment" => "ignore",
                _ => "binary"
            };
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(_jiraIdTextBox.Text))
                {
                    MessageBox.Show("Please enter a JIRA ID.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_descriptionTextBox.Text))
                {
                    MessageBox.Show("Please enter a description.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Collect selected projects
                var projects = new List<FeatureChainService.ProjectConfig>();
                foreach (DataGridViewRow row in _projectsGrid.Rows)
                {
                    try
                    {
                        var includeValue = false;
                        if (row.Cells["Include"]?.Value is bool include)
                            includeValue = include;
                        
                        if (includeValue)
                        {
                            var project = new FeatureChainService.ProjectConfig
                            {
                                ProjectName = row.Cells["Project"]?.Value?.ToString() ?? "",
                                Mode = row.Cells["Mode"]?.Value?.ToString() ?? "source",
                                DevMode = row.Cells["DevMode"]?.Value?.ToString() ?? "",
                                Branch = row.Cells["Branch"]?.Value?.ToString() ?? "",
                                Tag = row.Cells["Tag"]?.Value?.ToString() ?? "",
                                ForkRepository = row.Cells["Fork"]?.Value?.ToString() ?? "",
                                TestsEnabled = true
                            };

                            projects.Add(project);
                        }
                    }
                    catch { continue; }
                }

                if (!projects.Any())
                {
                    MessageBox.Show("Please select at least one project.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create feature chain request
                var request = new FeatureChainService.FeatureChainRequest
                {
                    JiraId = _jiraIdTextBox.Text.Trim(),
                    Description = _descriptionTextBox.Text.Trim(),
                    Version = _versionTextBox.Text.Trim(),
                    DevsVersion = _devsVersionTextBox.Text.Trim(),
                    Recipients = _recipientsTextBox.Text.Trim(),
                    IntegrationTag = $"Build_V12.0.2.{_devsVersionTextBox.Text.Trim()}",
                    Projects = projects
                };

                // Create the feature chain file
                var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FeatureChains");
                Directory.CreateDirectory(outputPath);

                CreatedFilePath = _featureService.CreateFeatureChainFile(request, outputPath);

                // Add integration tests to the file if any selected
                var selectedTests = _integrationTestsListView.CheckedItems.Cast<ListViewItem>()
                    .Select(item => item.Tag.ToString()).ToList();

                if (selectedTests.Any())
                {
                    AddIntegrationTestsToFile(CreatedFilePath, selectedTests);
                }

                MessageBox.Show($"Feature chain created successfully!\n\nFile: {CreatedFilePath}", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating feature chain: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProjectsGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == _projectsGrid.Columns["Fork"].Index && e.RowIndex >= 0)
            {
                var projectName = _projectsGrid.Rows[e.RowIndex].Cells["Project"].Value?.ToString();
                if (!string.IsNullOrEmpty(projectName))
                {
                    UpdateForkOptions(e.RowIndex, projectName);
                }
            }
        }

        private void UpdateForkOptions(int rowIndex, string projectName)
        {
            var forkCell = _projectsGrid.Rows[rowIndex].Cells["Fork"] as DataGridViewComboBoxCell;
            if (forkCell != null)
            {
                var config = ConfigurationLoader.LoadValidationConfig();
                var availableForks = new List<string> { "" };

                // Add forks that support this project in owner/project format
                foreach (var fork in config.KnownForks)
                {
                    if (fork.Value.Contains(projectName))
                    {
                        availableForks.Add($"{fork.Key}/{projectName}");
                    }
                }

                forkCell.Items.Clear();
                forkCell.Items.AddRange(availableForks.ToArray());
            }
        }

        private void AddIntegrationTestsToFile(string filePath, List<string> selectedTests)
        {
            try
            {
                var lines = File.ReadAllLines(filePath).ToList();
                
                // Add integration tests at the end
                lines.Add("");
                lines.Add("# Integration Tests");
                foreach (var test in selectedTests)
                {
                    lines.Add($"tests.{test}.run=true");
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Could not add integration tests to file: {ex.Message}", 
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}