using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.WinForms
{
    public partial class MainForm : Form
    {
        private readonly ChainFileParser _parser;
        private readonly ChainFileWriter _writer;
        private readonly ChainValidator _validator;
        private Panel _mainPanel = null!;
        private Label _statusLabel = null!;
        private string? _currentFile;

        public MainForm()
        {
            _parser = new ChainFileParser();
            _writer = new ChainFileWriter();
            var rules = ValidationRuleFactory.CreateAllRules();
            _validator = new ChainValidator(rules);
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = "Chain File Editor";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Global file selection panel
            var fileSelectionPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                BackColor = SystemColors.ControlLight,
                Padding = new Padding(10, 10, 10, 5)
            };

            var fileLabel = new Label
            {
                Text = "Selected File:",
                Location = new Point(10, 15),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var filePathLabel = new Label
            {
                Text = "No file selected",
                Location = new Point(100, 15),
                Size = new Size(600, 20),
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9)
            };

            var selectBtn = new Button
            {
                Text = "Select File",
                Location = new Point(720, 12),
                Size = new Size(100, 26),
                UseVisualStyleBackColor = true
            };
            selectBtn.Click += SelectBtn_Click;

            fileSelectionPanel.Controls.Add(fileLabel);
            fileSelectionPanel.Controls.Add(filePathLabel);
            fileSelectionPanel.Controls.Add(selectBtn);

            // Top toolbar
            var toolbar = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = SystemColors.Control
            };

            var operations = new[]
            {
                new { Name = "Validate", Tag = 1 },
                new { Name = "Rebase", Tag = 2 },
                new { Name = "Feature Chain", Tag = 3 },
                new { Name = "Branch", Tag = 4 },
                new { Name = "Modes", Tag = 5 },
                new { Name = "Tests", Tag = 6 },
                new { Name = "Current File", Tag = 7 }
            };

            int xPos = 20;
            foreach (var op in operations)
            {
                var btn = new Button
                {
                    Text = op.Name,
                    Size = new Size(120, 50),
                    Location = new Point(xPos, 15),
                    UseVisualStyleBackColor = true,
                    Font = new Font("Segoe UI", 10),
                    Tag = op.Tag
                };
                btn.Click += ToolbarButton_Click;
                toolbar.Controls.Add(btn);
                xPos += 130;
            }

            // Main panel for operation content
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            // Status bar
            var statusBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Bottom,
                BackColor = SystemColors.Control
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 9),
                Location = new Point(10, 8),
                Size = new Size(800, 15),
                ForeColor = SystemColors.ControlText
            };
            statusBar.Controls.Add(_statusLabel);

            this.Controls.Add(_mainPanel);
            this.Controls.Add(toolbar);
            this.Controls.Add(fileSelectionPanel);
            this.Controls.Add(statusBar);

            // Show default validate panel
            ShowValidatePanel();

            this.ResumeLayout(false);
        }

        private void ToolbarButton_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            var operation = (int)button.Tag;

            switch (operation)
            {
                case 1: ShowValidatePanel(); break;
                case 2: ShowRebasePanel(); break;
                case 3: ShowFeatureChainPanel(); break;
                case 4: ShowBranchPanel(); break;
                case 5: ShowModesPanel(); break;
                case 6: ShowTestsPanel(); break;
                case 7: ShowCurrentFilePanel(); break;
            }
        }

        private void ShowValidatePanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Validate Chain File",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(300, 30),
                ForeColor = SystemColors.ControlText
            };

            // Validation button
            var validateBtn = new Button
            {
                Text = "Run Validation",
                Location = new Point(50, 70),
                Size = new Size(150, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            validateBtn.Click += (s, e) => ValidateChain();

            // Validation status section
            var statusPanel = new Panel
            {
                Location = new Point(50, 120),
                Size = new Size(900, 80),
                BorderStyle = BorderStyle.FixedSingle
            };

            var statusTitle = new Label
            {
                Text = "Validation Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(150, 20)
            };

            var validationStatus = new Label
            {
                Text = "Status: Not Validated",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 35),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            var errorCountLabel = new Label
            {
                Text = "Errors: 0",
                Font = new Font("Segoe UI", 10),
                Location = new Point(220, 35),
                Size = new Size(80, 20),
                ForeColor = Color.Red
            };

            var warningCountLabel = new Label
            {
                Text = "Warnings: 0",
                Font = new Font("Segoe UI", 10),
                Location = new Point(310, 35),
                Size = new Size(100, 20),
                ForeColor = Color.Orange
            };

            var validCountLabel = new Label
            {
                Text = "Valid Items: 0",
                Font = new Font("Segoe UI", 10),
                Location = new Point(420, 35),
                Size = new Size(100, 20),
                ForeColor = Color.Green
            };

            var fileNameLabel = new Label
            {
                Text = string.IsNullOrEmpty(_currentFile) ? "File: No file selected" : $"File: {Path.GetFileName(_currentFile)}",
                Font = new Font("Segoe UI", 9),
                Location = new Point(10, 55),
                Size = new Size(880, 15),
                ForeColor = string.IsNullOrEmpty(_currentFile) ? Color.Red : Color.Gray
            };

            statusPanel.Controls.Add(statusTitle);
            statusPanel.Controls.Add(validationStatus);
            statusPanel.Controls.Add(errorCountLabel);
            statusPanel.Controls.Add(warningCountLabel);
            statusPanel.Controls.Add(validCountLabel);
            statusPanel.Controls.Add(fileNameLabel);

            // Results section with detailed validation display
            var resultsPanel = new Panel
            {
                Location = new Point(50, 220),
                Size = new Size(900, 300),
                BorderStyle = BorderStyle.FixedSingle
            };

            var resultsTitle = new Label
            {
                Text = "Validation Details",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(150, 20)
            };

            var validationDetailsTextBox = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(870, 255),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Run validation to see detailed results..."
            };

            resultsPanel.Controls.Add(resultsTitle);
            resultsPanel.Controls.Add(validationDetailsTextBox);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(validateBtn);
            _mainPanel.Controls.Add(statusPanel);
            _mainPanel.Controls.Add(resultsPanel);
        }

        private void ShowRebasePanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Update Version Properties",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(100, 20),
                Size = new Size(350, 30),
                ForeColor = SystemColors.ControlText
            };

            // Version input section
            var versionPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(800, 80),
                BorderStyle = BorderStyle.FixedSingle
            };

            var currentVersionLabel = new Label
            {
                Text = "Current Version:",
                Location = new Point(10, 15),
                Size = new Size(120, 20)
            };

            var currentVersionValue = new Label
            {
                Text = "Not loaded",
                Location = new Point(140, 15),
                Size = new Size(200, 20),
                ForeColor = Color.Blue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var newVersionLabel = new Label
            {
                Text = "New Version:",
                Location = new Point(10, 45),
                Size = new Size(100, 20)
            };

            var newVersionTextBox = new TextBox
            {
                Location = new Point(120, 43),
                Size = new Size(200, 25),
                PlaceholderText = "Enter new version (e.g., 123)"
            };

            versionPanel.Controls.Add(currentVersionLabel);
            versionPanel.Controls.Add(currentVersionValue);
            versionPanel.Controls.Add(newVersionLabel);
            versionPanel.Controls.Add(newVersionTextBox);

            // Project selection grid
            var gridPanel = new Panel
            {
                Location = new Point(50, 150),
                Size = new Size(800, 300),
                BorderStyle = BorderStyle.FixedSingle
            };

            var gridTitle = new Label
            {
                Text = "Projects in File",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            var projectGrid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(770, 220),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Select",
                FillWeight = 12
            };
            var projectColumn = new DataGridViewTextBoxColumn
            {
                Name = "Project",
                HeaderText = "Project",
                ReadOnly = true,
                FillWeight = 20
            };
            var propertyColumn = new DataGridViewTextBoxColumn
            {
                Name = "Property",
                HeaderText = "Property Type",
                ReadOnly = true,
                FillWeight = 18
            };
            var currentColumn = new DataGridViewTextBoxColumn
            {
                Name = "Current",
                HeaderText = "Current Value",
                ReadOnly = true,
                FillWeight = 25
            };
            var statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                ReadOnly = true,
                FillWeight = 25
            };

            projectGrid.Columns.Add(selectColumn);
            projectGrid.Columns.Add(projectColumn);
            projectGrid.Columns.Add(propertyColumn);
            projectGrid.Columns.Add(currentColumn);
            projectGrid.Columns.Add(statusColumn);

            var selectAllBtn = new Button
            {
                Text = "Select All",
                Location = new Point(10, 270),
                Size = new Size(80, 25),
                UseVisualStyleBackColor = true
            };
            selectAllBtn.Click += (s, e) => {
                foreach (DataGridViewRow row in projectGrid.Rows)
                    row.Cells["Select"].Value = true;
            };

            var clearAllBtn = new Button
            {
                Text = "Clear All",
                Location = new Point(100, 270),
                Size = new Size(80, 25),
                UseVisualStyleBackColor = true
            };
            clearAllBtn.Click += (s, e) => {
                foreach (DataGridViewRow row in projectGrid.Rows)
                    row.Cells["Select"].Value = false;
            };

            var loadCurrentBtn = new Button
            {
                Text = "Load File Projects",
                Location = new Point(190, 270),
                Size = new Size(130, 25),
                UseVisualStyleBackColor = true
            };
            loadCurrentBtn.Click += (s, e) => LoadFileProjects(projectGrid, currentVersionValue);

            gridPanel.Controls.Add(gridTitle);
            gridPanel.Controls.Add(projectGrid);
            gridPanel.Controls.Add(selectAllBtn);
            gridPanel.Controls.Add(clearAllBtn);
            gridPanel.Controls.Add(loadCurrentBtn);
            
            // Auto-load projects if file is already selected
            if (!string.IsNullOrEmpty(_currentFile))
            {
                LoadFileProjects(projectGrid, currentVersionValue);
            }

            // Update button
            var updateBtn = new Button
            {
                Text = "Update Selected",
                Location = new Point(50, 470),
                Size = new Size(150, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            updateBtn.Click += (s, e) => PerformTabularVersionUpdate(newVersionTextBox, projectGrid);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(versionPanel);
            _mainPanel.Controls.Add(gridPanel);
            _mainPanel.Controls.Add(updateBtn);
        }

        private void ShowFeatureChainPanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Create Feature Chain",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(300, 30),
                ForeColor = SystemColors.ControlText
            };

            // Basic info panel
            var infoPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(900, 120),
                BorderStyle = BorderStyle.FixedSingle
            };

            var jiraLabel = new Label { Text = "JIRA ID: *", Location = new Point(10, 15), Size = new Size(80, 20) };
            var jiraTextBox = new TextBox { Location = new Point(100, 13), Size = new Size(150, 25), PlaceholderText = "e.g., 12345" };

            var descLabel = new Label { Text = "Description: *", Location = new Point(10, 50), Size = new Size(80, 20) };
            var descTextBox = new TextBox { Location = new Point(100, 48), Size = new Size(400, 25), PlaceholderText = "Feature description" };

            var versionLabel = new Label { Text = "Version:", Location = new Point(10, 85), Size = new Size(80, 20) };
            var versionTextBox = new TextBox { Location = new Point(100, 83), Size = new Size(150, 25), PlaceholderText = "Required if binary mode used" };

            infoPanel.Controls.AddRange(new Control[] { jiraLabel, jiraTextBox, descLabel, descTextBox, versionLabel, versionTextBox });

            // Projects panel
            var projectsPanel = new Panel
            {
                Location = new Point(50, 210),
                Size = new Size(900, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            var projectsTitle = new Label
            {
                Text = "Project Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            var projectsGrid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(870, 160),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var includeCol = new DataGridViewCheckBoxColumn { Name = "Include", HeaderText = "Include", FillWeight = 8 };
            var projectCol = new DataGridViewComboBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 20 };
            var modeCol = new DataGridViewComboBoxColumn { Name = "Mode", HeaderText = "Mode", FillWeight = 12 };
            var devModeCol = new DataGridViewComboBoxColumn { Name = "DevMode", HeaderText = "Dev Mode", FillWeight = 12 };
            var branchCol = new DataGridViewComboBoxColumn { Name = "Branch", HeaderText = "Branch", FillWeight = 12 };
            branchCol.Items.AddRange(new[] { "", "main", "stage", "integration" });
            var tagCol = new DataGridViewComboBoxColumn { Name = "Tag", HeaderText = "Tag", FillWeight = 12 };
            tagCol.Items.AddRange(new[] { "", "Build_1.0.0.1", "Build_1.0.0.2", "Build_1.0.0.3" });
            var testsCol = new DataGridViewCheckBoxColumn { Name = "Tests", HeaderText = "Tests", FillWeight = 8 };
            var forkCol = new DataGridViewComboBoxColumn { Name = "Fork", HeaderText = "Fork Repository", FillWeight = 16 };
            forkCol.Items.AddRange(new[] { "", "petr.novacek", "ivan.rebo", "dasa.petrezselyova", "vojtech.lahoda", "vit.holy", "stefan.kiel", "oliver.schmidt" });

            var mainRepos = new[] { "administration", "appengine", "appstudio", "consolidation", "content", "content-backup", "deployment", "dashboards", "depmservice", "designer", "framework", "modeling", "officeinteg", "olap", "olap-backup", "repository", "tests" };
            projectCol.Items.AddRange(mainRepos);
            modeCol.Items.AddRange(new[] { "source", "binary", "ignore" });
            devModeCol.Items.AddRange(new[] { "", "binary", "ignore" });

            projectsGrid.Columns.AddRange(new DataGridViewColumn[] { includeCol, projectCol, modeCol, devModeCol, branchCol, tagCol, testsCol, forkCol });

            // Handle cell value changes to update branches and fork format
            projectsGrid.CellValueChanged += (s, e) => {
                try
                {
                    if (e.RowIndex >= 0 && e.RowIndex < projectsGrid.Rows.Count)
                    {
                        var row = projectsGrid.Rows[e.RowIndex];
                        var project = row.Cells["Project"].Value?.ToString();
                        var fork = row.Cells["Fork"].Value?.ToString();
                        
                        // Auto-format fork to owner/project when fork owner is selected
                        if (e.ColumnIndex == forkCol.Index && !string.IsNullOrEmpty(fork) && !fork.Contains("/") && !string.IsNullOrEmpty(project))
                        {
                            row.Cells["Fork"].Value = $"{fork}/{project}";
                        }
                        
                        // Update branch options
                        if (e.ColumnIndex == projectCol.Index || e.ColumnIndex == forkCol.Index)
                        {
                            var branchCell = row.Cells["Branch"] as DataGridViewComboBoxCell;
                            if (branchCell != null)
                            {
                                branchCell.Items.Clear();
                                branchCell.Items.AddRange(new[] { "", "main", "stage", "integration" });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently handle grid exceptions
                }
            };

            projectsGrid.CurrentCellDirtyStateChanged += (s, e) => {
                if (projectsGrid.IsCurrentCellDirty)
                {
                    projectsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            
            projectsGrid.DataError += (s, e) => {
                e.Cancel = true;
            };

            // Auto-load all projects with proper defaults
            try
            {
                foreach (var repo in mainRepos)
                {
                    var defaultMode = "source";
                    var defaultDevMode = repo switch { "designer" => "ignore", "deployment" => "ignore", "tests" => "ignore", _ => "binary" };
                    var defaultBranch = "main";
                    var defaultTests = repo == "tests"; // Tests project defaults to enabled
                    
                    var rowIndex = projectsGrid.Rows.Add(true, repo, defaultMode, defaultDevMode, defaultBranch, "", defaultTests, "");
                    var branchCell = projectsGrid.Rows[rowIndex].Cells["Branch"] as DataGridViewComboBoxCell;
                    if (branchCell != null)
                    {
                        branchCell.Items.Clear();
                        branchCell.Items.Add("");
                        var branches = GetProjectBranches(repo);
                        // Rule 6: Content project cannot have stage branch
                        if (repo == "content")
                        {
                            branches = branches.Where(b => b != "stage").ToArray();
                        }
                        branchCell.Items.AddRange(branches);
                        branchCell.Value = defaultBranch;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Grid Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            // Auto-update branch names when JIRA ID or description changes
            EventHandler updateBranches = (s, e) => {
                if (!string.IsNullOrWhiteSpace(jiraTextBox.Text) && !string.IsNullOrWhiteSpace(descTextBox.Text))
                {
                    var featureBranch = $"dev/DEPM-{jiraTextBox.Text.Trim()}-{SanitizeForBranch(descTextBox.Text.Trim())}";
                    foreach (DataGridViewRow row in projectsGrid.Rows)
                    {
                        var includeValue = false;
                        if (row.Cells["Include"]?.Value is bool include)
                            includeValue = include;
                            
                        if (includeValue)
                        {
                            var branchCell = row.Cells["Branch"] as DataGridViewComboBoxCell;
                            if (branchCell != null && !branchCell.Items.Contains(featureBranch))
                            {
                                branchCell.Items.Add(featureBranch);
                            }
                        }
                    }
                }
            };
            
            jiraTextBox.TextChanged += updateBranches;
            descTextBox.TextChanged += updateBranches;

            projectsPanel.Controls.Add(projectsTitle);
            projectsPanel.Controls.Add(projectsGrid);

            // Buttons panel
            var buttonsPanel = new Panel
            {
                Location = new Point(50, 470),
                Size = new Size(900, 50),
                BackColor = Color.Transparent
            };

            // Save button
            var saveBtn = new Button
            {
                Text = "Save Feature Chain",
                Location = new Point(0, 10),
                Size = new Size(180, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = SystemColors.ButtonFace
            };
            saveBtn.Click += (s, e) => CreateFeatureChain(jiraTextBox, descTextBox, versionTextBox, projectsGrid);

            buttonsPanel.Controls.Add(saveBtn);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(infoPanel);
            _mainPanel.Controls.Add(projectsPanel);
            _mainPanel.Controls.Add(buttonsPanel);
        }

        private void ShowBranchPanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Manage Project Branches",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(350, 30),
                ForeColor = SystemColors.ControlText
            };

            // Projects grid panel
            var projectsPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(900, 350),
                BorderStyle = BorderStyle.FixedSingle
            };

            var projectsTitle = new Label
            {
                Text = "All Projects in File",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            var projectsGrid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(870, 270),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var projectCol = new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project Name", ReadOnly = true, FillWeight = 25 };
            var currentBranchCol = new DataGridViewTextBoxColumn { Name = "CurrentBranch", HeaderText = "Current Branch", ReadOnly = true, FillWeight = 25 };
            var statusCol = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", ReadOnly = true, FillWeight = 20 };
            var newBranchCol = new DataGridViewTextBoxColumn { Name = "NewBranch", HeaderText = "New Branch Name", FillWeight = 30 };

            projectsGrid.Columns.AddRange(new DataGridViewColumn[] { projectCol, currentBranchCol, statusCol, newBranchCol });

            var loadBtn = new Button
            {
                Text = "Load Projects",
                Location = new Point(10, 320),
                Size = new Size(120, 25),
                UseVisualStyleBackColor = true
            };
            loadBtn.Click += (s, e) => LoadAllProjectBranches(projectsGrid);

            projectsPanel.Controls.Add(projectsTitle);
            projectsPanel.Controls.Add(projectsGrid);
            projectsPanel.Controls.Add(loadBtn);

            // Update button
            var updateBtn = new Button
            {
                Text = "Update Branches",
                Location = new Point(50, 440),
                Size = new Size(150, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            updateBtn.Click += (s, e) => UpdateAllBranches(projectsGrid);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(projectsPanel);
            _mainPanel.Controls.Add(updateBtn);
        }

        private void ShowModesPanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Manage Project Modes",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(350, 30),
                ForeColor = SystemColors.ControlText
            };

            // Projects grid panel
            var projectsPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(900, 350),
                BorderStyle = BorderStyle.FixedSingle
            };

            var projectsTitle = new Label
            {
                Text = "All Projects in File",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            var projectsGrid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(870, 270),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var projectCol = new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project Name", ReadOnly = true, FillWeight = 25 };
            var currentModeCol = new DataGridViewTextBoxColumn { Name = "CurrentMode", HeaderText = "Current Mode", ReadOnly = true, FillWeight = 20 };
            var currentDevModeCol = new DataGridViewTextBoxColumn { Name = "CurrentDevMode", HeaderText = "Current Dev Mode", ReadOnly = true, FillWeight = 20 };
            var newModeCol = new DataGridViewComboBoxColumn { Name = "NewMode", HeaderText = "New Mode", FillWeight = 17 };
            var newDevModeCol = new DataGridViewComboBoxColumn { Name = "NewDevMode", HeaderText = "New Dev Mode", FillWeight = 18 };

            var modeService = new ModeService();
            var validModes = modeService.GetValidModes().ToList();
            validModes.Insert(0, "");
            
            var devModeOptions = new List<string> { "", "(clear)" };
            devModeOptions.AddRange(modeService.GetValidModes());

            newModeCol.Items.AddRange(validModes.ToArray());
            newDevModeCol.Items.AddRange(devModeOptions.ToArray());

            projectsGrid.Columns.AddRange(new DataGridViewColumn[] { projectCol, currentModeCol, currentDevModeCol, newModeCol, newDevModeCol });

            var loadBtn = new Button
            {
                Text = "Load Projects",
                Location = new Point(10, 320),
                Size = new Size(120, 25),
                UseVisualStyleBackColor = true
            };
            loadBtn.Click += (s, e) => LoadAllProjectModes(projectsGrid);

            projectsPanel.Controls.Add(projectsTitle);
            projectsPanel.Controls.Add(projectsGrid);
            projectsPanel.Controls.Add(loadBtn);

            // Update button
            var updateBtn = new Button
            {
                Text = "Update Modes",
                Location = new Point(50, 440),
                Size = new Size(150, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            updateBtn.Click += (s, e) => UpdateAllModes(projectsGrid);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(projectsPanel);
            _mainPanel.Controls.Add(updateBtn);
        }

        private void ShowTestsPanel()
        {
            _mainPanel.Controls.Clear();

            var title = new Label
            {
                Text = "Manage Project Tests",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(350, 30),
                ForeColor = SystemColors.ControlText
            };

            // Projects grid panel
            var projectsPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(900, 350),
                BorderStyle = BorderStyle.FixedSingle
            };

            var projectsTitle = new Label
            {
                Text = "All Projects in File",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            var projectsGrid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(870, 270),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var projectCol = new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project Name", ReadOnly = true, FillWeight = 40 };
            var currentStatusCol = new DataGridViewTextBoxColumn { Name = "CurrentStatus", HeaderText = "Current Status", ReadOnly = true, FillWeight = 30 };
            var newStatusCol = new DataGridViewCheckBoxColumn { Name = "NewStatus", HeaderText = "Enable Tests", FillWeight = 30 };

            projectsGrid.Columns.AddRange(new DataGridViewColumn[] { projectCol, currentStatusCol, newStatusCol });

            var loadBtn = new Button
            {
                Text = "Load Projects",
                Location = new Point(10, 320),
                Size = new Size(120, 25),
                UseVisualStyleBackColor = true
            };
            loadBtn.Click += (s, e) => LoadAllProjectTests(projectsGrid);

            projectsPanel.Controls.Add(projectsTitle);
            projectsPanel.Controls.Add(projectsGrid);
            projectsPanel.Controls.Add(loadBtn);

            // Update button
            var updateBtn = new Button
            {
                Text = "Update Tests",
                Location = new Point(50, 440),
                Size = new Size(150, 35),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            updateBtn.Click += (s, e) => UpdateAllTests(projectsGrid);

            _mainPanel.Controls.Add(title);
            _mainPanel.Controls.Add(projectsPanel);
            _mainPanel.Controls.Add(updateBtn);
        }

        private void ShowCurrentFilePanel()
        {
            _mainPanel.Controls.Clear();

            var contentTextBox = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(950, 520),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                Text = GetFileContent(),
                BorderStyle = BorderStyle.FixedSingle
            };

            _mainPanel.Controls.Add(contentTextBox);
        }



        private string GetFileContent()
        {
            if (string.IsNullOrEmpty(_currentFile))
                return "No file selected. Use the global file selection at the top to choose a file.";
            
            if (!File.Exists(_currentFile))
                return "Selected file does not exist.";
            
            try
            {
                return File.ReadAllText(_currentFile);
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

        private List<string> _allFiles = new List<string>();

        private void LoadFiles(ListBox fileListBox, ComboBox filterCombo, TextBox searchBox)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select folder containing chain files";
                folderDialog.ShowNewFolderButton = false;
                
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _allFiles.Clear();
                        _allFiles.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => !Path.GetFileName(f).StartsWith("."))); // Skip hidden files
                        FilterFileList(fileListBox, filterCombo, searchBox);
                        _statusLabel.Text = $"Loaded {_allFiles.Count} files from {folderDialog.SelectedPath}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void FilterFileList(ListBox fileListBox, ComboBox filterCombo, TextBox searchBox)
        {
            if (_allFiles == null || _allFiles.Count == 0) return;
            
            fileListBox.Items.Clear();
            
            var filteredFiles = _allFiles.AsEnumerable();
            
            // Filter by file type
            switch (filterCombo.SelectedIndex)
            {
                case 1: // *.properties
                    filteredFiles = filteredFiles.Where(f => f.EndsWith(".properties", StringComparison.OrdinalIgnoreCase));
                    break;
                case 2: // *.chain
                    filteredFiles = filteredFiles.Where(f => f.EndsWith(".chain", StringComparison.OrdinalIgnoreCase));
                    break;
                case 3: // *.config
                    filteredFiles = filteredFiles.Where(f => f.EndsWith(".config", StringComparison.OrdinalIgnoreCase) || 
                                                           f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                                           f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                    break;
            }
            
            // Filter by search text
            if (!string.IsNullOrWhiteSpace(searchBox.Text))
            {
                var searchText = searchBox.Text.ToLower();
                filteredFiles = filteredFiles.Where(f => Path.GetFileName(f).ToLower().Contains(searchText));
            }
            
            var sortedFiles = filteredFiles.OrderBy(f => Path.GetFileName(f)).ToList();
            
            foreach (var file in sortedFiles)
            {
                fileListBox.Items.Add(file);
            }
            
            _statusLabel.Text = $"Showing {sortedFiles.Count} of {_allFiles.Count} files";
        }

        private string GenerateValidationDetails(ChainFileEditor.Core.Validation.ValidationReport report, ChainFileEditor.Core.Models.ChainModel chain, string filePath)
        {
            var details = new System.Text.StringBuilder();
            
            // Summary Section
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            details.AppendLine("                              VALIDATION SUMMARY");
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            details.AppendLine();
            
            var errorCount = report.Issues.Count(i => i.Severity == ChainFileEditor.Core.Validation.ValidationSeverity.Error);
            var warningCount = report.Issues.Count(i => i.Severity == ChainFileEditor.Core.Validation.ValidationSeverity.Warning);
            var totalIssues = report.Issues.Count();
            
            details.AppendLine($"File: {System.IO.Path.GetFileName(filePath)}");
            details.AppendLine($"Validation Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            details.AppendLine($"Total Issues Found: {totalIssues}");
            details.AppendLine($"  • Errors: {errorCount}");
            details.AppendLine($"  • Warnings: {warningCount}");
            
            var status = errorCount > 0 ? "FAILED" : (warningCount > 0 ? "PASSED WITH WARNINGS" : "PASSED");
            details.AppendLine($"Overall Status: {status}");
            details.AppendLine();
            
            // Project Analysis Section
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            details.AppendLine("                             PROJECT ANALYSIS");
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            details.AppendLine();
            
            details.AppendLine($"Global Configuration:");
            details.AppendLine($"  • Global Version: {chain.Global?.Version ?? "Not specified"}");
            details.AppendLine($"  • Global Description: {chain.Global?.Description ?? "Not specified"}");
            details.AppendLine($"  • JIRA ID: {chain.Global?.JiraId ?? "Not specified"}");
            details.AppendLine();
            
            details.AppendLine($"Projects Found: {chain.Sections?.Count ?? 0}");
            if (chain.Sections?.Any() == true)
            {
                foreach (var section in chain.Sections.Take(10)) // Show first 10 projects
                {
                    details.AppendLine($"  • {section.Name}:");
                    details.AppendLine($"    - Mode: {section.Mode ?? "Not specified"}");
                    details.AppendLine($"    - Branch: {section.Branch ?? "Not specified"}");
                    details.AppendLine($"    - Tag: {section.Tag ?? "Not specified"}");
                    details.AppendLine($"    - Tests: {(section.TestsUnit ? "Enabled" : "Disabled")}");
                }
                
                if (chain.Sections.Count > 10)
                {
                    details.AppendLine($"  ... and {chain.Sections.Count - 10} more projects");
                }
            }
            details.AppendLine();
            
            // Errors and Warnings Section
            if (totalIssues > 0)
            {
                details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
                details.AppendLine("                            ERRORS AND WARNINGS");
                details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
                details.AppendLine();
                
                // Group issues by severity
                var errorIssues = report.Issues.Where(i => i.Severity == ChainFileEditor.Core.Validation.ValidationSeverity.Error).ToList();
                var warningIssues = report.Issues.Where(i => i.Severity == ChainFileEditor.Core.Validation.ValidationSeverity.Warning).ToList();
                
                if (errorIssues.Any())
                {
                    details.AppendLine($"🔴 ERRORS ({errorIssues.Count}):");
                    details.AppendLine("───────────────────────────────────────────────────────────────────────────────");
                    
                    for (int i = 0; i < errorIssues.Count; i++)
                    {
                        var issue = errorIssues[i];
                        details.AppendLine($"{i + 1}. Rule: {issue.RuleId}");
                        details.AppendLine($"   Section: {issue.SectionName ?? "Global"}");
                        details.AppendLine($"   Issue: {GetClearReason(issue.RuleId, issue.Message)}");
                        details.AppendLine();
                    }
                }
                
                if (warningIssues.Any())
                {
                    details.AppendLine($"🟡 WARNINGS ({warningIssues.Count}):");
                    details.AppendLine("───────────────────────────────────────────────────────────────────────────────");
                    
                    for (int i = 0; i < warningIssues.Count; i++)
                    {
                        var issue = warningIssues[i];
                        details.AppendLine($"{i + 1}. Rule: {issue.RuleId}");
                        details.AppendLine($"   Section: {issue.SectionName ?? "Global"}");
                        details.AppendLine($"   Issue: {GetClearReason(issue.RuleId, issue.Message)}");
                        details.AppendLine();
                    }
                }
            }
            else
            {
                details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
                details.AppendLine("                               ALL CHECKS PASSED");
                details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
                details.AppendLine();
                details.AppendLine("✅ No validation issues found. The chain file is valid!");
                details.AppendLine();
            }
            
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            details.AppendLine("                              END OF REPORT");
            details.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            
            return details.ToString();
        }
        
        private string GetClearReason(string ruleId, string message)
        {
            var reasons = new Dictionary<string, string>
            {
                ["ModeRequired"] = "Project missing required mode specification (source/binary/fork)",
                ["VersionConsistency"] = "Version mismatch detected between projects",
                ["BranchOrTag"] = "Project must specify either branch or tag reference",
                ["RequiredProjects"] = "Essential project dependencies are missing",
                ["ProjectNaming"] = "Project name does not follow naming convention",
                ["ForkValidation"] = "Invalid fork repository or branch reference",
                ["TestsPreferBranch"] = "Test project should use branch instead of tag",
                ["DevModeOverride"] = "Development mode override in production config",
                ["GlobalVersionWhenBinary"] = "Binary project missing global version specification",
                ["GitRepositoryValidation"] = "Invalid or inaccessible Git repository URL",
                ["ContentNotStage"] = "Staging environment reference in production config",
                ["CommentedOutSection"] = "Commented configuration section needs review",
                ["FeatureForkRecommendation"] = "Feature branch should consider using fork mode"
            };
            
            return reasons.ContainsKey(ruleId) ? reasons[ruleId] : message;
        }

        private void ValidateChain()
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a chain file first.\n\n1. Click 'Browse Folder' to load files\n2. Select a file from the list\n3. Click 'Run Validation'", 
                    "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(_currentFile))
            {
                MessageBox.Show($"Selected file does not exist: {_currentFile}", 
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _statusLabel.Text = "Running validation...";
                
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var report = _validator.Validate(chain);
                
                var errorCount = report.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                var warningCount = report.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
                var totalIssues = report.Issues.Count();
                var validCount = Math.Max(0, 10 - totalIssues); // Assuming 10 total validation points
                
                _statusLabel.Text = $"Validation complete: {errorCount} errors, {warningCount} warnings";
                
                // Update status panel
                var statusPanel = _mainPanel.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text.StartsWith("Status:")));
                if (statusPanel != null)
                {
                    var statusLabel = statusPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Status:"));
                    var errorLabel = statusPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Errors:"));
                    var warningLabel = statusPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Warnings:"));
                    var validLabel = statusPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Valid Items:"));
                    var fileLabel = statusPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("File:"));
                    
                    if (statusLabel != null)
                    {
                        var status = errorCount > 0 ? "FAILED" : (warningCount > 0 ? "PASSED WITH WARNINGS" : "PASSED");
                        statusLabel.Text = $"Status: {status}";
                        statusLabel.ForeColor = errorCount > 0 ? Color.Red : (warningCount > 0 ? Color.Orange : Color.Green);
                    }
                    
                    if (errorLabel != null) errorLabel.Text = $"Errors: {errorCount}";
                    if (warningLabel != null) warningLabel.Text = $"Warnings: {warningCount}";
                    if (validLabel != null) validLabel.Text = $"Valid Items: {validCount}";
                    if (fileLabel != null) fileLabel.Text = $"File: {Path.GetFileName(_currentFile)}";
                }
                
                // Find the results text box and update with detailed validation information
                var resultsPanel = _mainPanel.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.OfType<TextBox>().Any(t => t.Multiline));
                var validationDetailsTextBox = resultsPanel?.Controls.OfType<TextBox>().FirstOrDefault(t => t.Multiline);
                
                if (validationDetailsTextBox != null)
                {
                    var detailsText = GenerateValidationDetails(report, chain, _currentFile);
                    validationDetailsTextBox.Text = detailsText;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Validation failed";
                MessageBox.Show($"Validation error: {ex.Message}\n\nFile: {_currentFile}", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCurrentVersion(Label versionLabel)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first from the Validate tab.", "No File Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var version = ExtractVersionFromChain(chain);
                versionLabel.Text = version ?? "No version found";
                versionLabel.ForeColor = version != null ? Color.Blue : Color.Red;
                _statusLabel.Text = $"Loaded version: {version ?? "None"} from {Path.GetFileName(_currentFile)}";
            }
            catch (Exception ex)
            {
                versionLabel.Text = "Error loading";
                versionLabel.ForeColor = Color.Red;
                MessageBox.Show($"Error loading version: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformTabularVersionUpdate(TextBox newVersionTextBox, DataGridView projectGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(newVersionTextBox.Text))
            {
                MessageBox.Show("Please enter a new version.", "Version Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newVersion = newVersionTextBox.Text.Trim();
            
            // Check version range (10000-30000)
            if (!RebaseService.IsVersionInValidRange(newVersion))
            {
                var warningMessage = RebaseService.GetVersionRangeWarningMessage(newVersion);
                var versionResult = MessageBox.Show(warningMessage, "Version Range Warning", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (versionResult == DialogResult.No)
                    return;
            }
            var selectedProjects = new List<string>();
            
            foreach (DataGridViewRow row in projectGrid.Rows)
            {
                var selectValue = false;
                if (row.Cells["Select"]?.Value is bool select)
                    selectValue = select;
                    
                if (selectValue)
                {
                    selectedProjects.Add(row.Cells["Project"]?.Value?.ToString() ?? "");
                }
            }

            if (!selectedProjects.Any())
            {
                MessageBox.Show("Please select at least one project to update.", 
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Update {selectedProjects.Count} projects to version {newVersion}?", 
                "Confirm Version Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    var updatesCount = UpdateTabularVersionsInFile(newVersion, selectedProjects);
                    _statusLabel.Text = $"Successfully updated {updatesCount} version properties";
                    
                    MessageBox.Show($"Version update completed!\n\n{updatesCount} properties updated to {newVersion}", 
                        "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                    LoadFileProjects(projectGrid, _mainPanel.Controls.OfType<Panel>().SelectMany(p => p.Controls.OfType<Label>()).FirstOrDefault(l => l.Text != "Not loaded" && l.ForeColor == Color.Blue));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating versions: {ex.Message}", "Update Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadFileProjects(DataGridView projectGrid, Label currentVersionValue)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var rebaseService = new RebaseService();
                
                // Get current version
                var currentVersion = rebaseService.ExtractCurrentVersion(chain);
                currentVersionValue.Text = currentVersion;
                currentVersionValue.ForeColor = currentVersion != "Not found" ? Color.Blue : Color.Red;
                
                // Get project analysis
                var projects = rebaseService.AnalyzeProjectVersions(chain);
                
                projectGrid.Rows.Clear();
                foreach (var project in projects)
                {
                    var isSelected = project.HasTag;
                    projectGrid.Rows.Add(isSelected, project.ProjectName, project.PropertyType, project.CurrentValue, project.Status);
                }
                
                _statusLabel.Text = $"Loaded {projects.Count} project properties from file. Current version: {currentVersion}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file projects: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int UpdateTabularVersionsInFile(string newVersion, List<string> selectedProjects)
        {
            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var rebaseService = new RebaseService();
                
                var updatesCount = rebaseService.UpdateSelectedProjects(chain, newVersion, selectedProjects);
                
                _writer.WritePropertiesFile(_currentFile, chain);
                return updatesCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update versions: {ex.Message}", ex);
            }
        }

        private string ExtractVersionFromChain(ChainFileEditor.Core.Models.ChainModel chain)
        {
            // Simple version extraction from file content
            var content = File.ReadAllText(_currentFile);
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.Contains("version="))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                        return parts[1].Trim();
                }
                if (line.Contains("globalVersion="))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                        return parts[1].Trim();
                }
            }
            return null;
        }

        private Panel CreateFileSelectionPanel(int x, int y)
        {
            var filePanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(800, 100),
                BorderStyle = BorderStyle.FixedSingle
            };

            var fileLabel = new Label
            {
                Text = "Selected File:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 15),
                Size = new Size(100, 20)
            };

            var filePathLabel = new Label
            {
                Text = _currentFile ?? "No file selected",
                Location = new Point(120, 15),
                Size = new Size(650, 20),
                ForeColor = string.IsNullOrEmpty(_currentFile) ? Color.Red : SystemColors.ControlText
            };

            var selectFileBtn = new Button
            {
                Text = "Select File",
                Location = new Point(10, 45),
                Size = new Size(100, 25),
                UseVisualStyleBackColor = true
            };
            selectFileBtn.Click += (s, e) => SelectGlobalFile(filePathLabel);

            var browseBtn = new Button
            {
                Text = "Browse Folder",
                Location = new Point(120, 45),
                Size = new Size(120, 25),
                UseVisualStyleBackColor = true
            };
            browseBtn.Click += (s, e) => BrowseGlobalFolder(filePathLabel);

            filePanel.Controls.Add(fileLabel);
            filePanel.Controls.Add(filePathLabel);
            filePanel.Controls.Add(selectFileBtn);
            filePanel.Controls.Add(browseBtn);

            return filePanel;
        }

        private void SelectGlobalFile(Label filePathLabel)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Chain files (*.properties;*.chain)|*.properties;*.chain|Properties files (*.properties)|*.properties|Chain files (*.chain)|*.chain|Configuration files (*.config;*.json;*.xml)|*.config;*.json;*.xml|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select Chain File";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _currentFile = openFileDialog.FileName;
                    filePathLabel.Text = _currentFile;
                    filePathLabel.ForeColor = SystemColors.ControlText;
                    _statusLabel.Text = $"File selected: {Path.GetFileName(_currentFile)}";
                }
            }
        }

        private void ShowFileSelector(Label filePathLabel)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Select Chain File";
                dialog.Size = new Size(600, 500);
                dialog.StartPosition = FormStartPosition.CenterParent;
                
                // Search box
                var searchLabel = new Label { Text = "Search:", Location = new Point(10, 15), Size = new Size(50, 20) };
                var searchBox = new TextBox { Location = new Point(65, 13), Size = new Size(200, 25) };
                
                var listBox = new ListBox
                {
                    Location = new Point(10, 50),
                    Size = new Size(570, 360),
                    Font = new Font("Segoe UI", 10)
                };
                
                string[] allFiles = null;
                try
                {
                    var defaultPath = @"C:\ChainFileEditor\Tests\Chains";
                    if (Directory.Exists(defaultPath))
                    {
                        allFiles = Directory.GetFiles(defaultPath, "*.*")
                            .Where(f => f.EndsWith(".properties", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".chain", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(f => Path.GetFileName(f))
                            .ToArray();
                        
                        foreach (var file in allFiles)
                        {
                            listBox.Items.Add(Path.GetFileName(file));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Search functionality
                searchBox.TextChanged += (s, e) => {
                    listBox.Items.Clear();
                    var searchText = searchBox.Text.ToLower();
                    foreach (var file in allFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        if (string.IsNullOrEmpty(searchText) || fileName.ToLower().Contains(searchText))
                        {
                            listBox.Items.Add(fileName);
                        }
                    }
                };
                
                var okBtn = new Button { Text = "OK", Location = new Point(420, 420), Size = new Size(75, 25), DialogResult = DialogResult.OK };
                var cancelBtn = new Button { Text = "Cancel", Location = new Point(505, 420), Size = new Size(75, 25), DialogResult = DialogResult.Cancel };
                
                dialog.Controls.AddRange(new Control[] { searchLabel, searchBox, listBox, okBtn, cancelBtn });
                
                listBox.DoubleClick += (s, e) => { if (listBox.SelectedItem != null) dialog.DialogResult = DialogResult.OK; };
                
                if (dialog.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
                {
                    var selectedFile = allFiles.FirstOrDefault(f => Path.GetFileName(f) == listBox.SelectedItem.ToString());
                    if (selectedFile != null)
                    {
                        _currentFile = selectedFile;
                        filePathLabel.Text = Path.GetFileName(selectedFile);
                        filePathLabel.ForeColor = SystemColors.ControlText;
                        _statusLabel.Text = $"File selected: {Path.GetFileName(selectedFile)}";
                    }
                }
            }
        }

        private void BrowseGlobalFolder(Label filePathLabel)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select folder containing chain files";
                folderDialog.ShowNewFolderButton = false;
                
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = Directory.GetFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".properties", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".chain", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(f => Path.GetFileName(f))
                        .ToArray();
                        
                    if (files.Length > 0)
                    {
                        using (var selectDialog = new Form())
                        {
                            selectDialog.Text = "Select File";
                            selectDialog.Size = new Size(600, 400);
                            selectDialog.StartPosition = FormStartPosition.CenterParent;
                            
                            var listBox = new ListBox
                            {
                                Dock = DockStyle.Fill,
                                Font = new Font("Consolas", 9)
                            };
                            
                            foreach (var file in files)
                                listBox.Items.Add(file);
                                
                            var buttonPanel = new Panel { Height = 40, Dock = DockStyle.Bottom };
                            var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(10, 8) };
                            var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(100, 8) };
                            
                            buttonPanel.Controls.Add(okBtn);
                            buttonPanel.Controls.Add(cancelBtn);
                            selectDialog.Controls.Add(listBox);
                            selectDialog.Controls.Add(buttonPanel);
                            
                            if (selectDialog.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
                            {
                                _currentFile = listBox.SelectedItem.ToString();
                                filePathLabel.Text = _currentFile;
                                filePathLabel.ForeColor = SystemColors.ControlText;
                                _statusLabel.Text = $"File selected: {Path.GetFileName(_currentFile)}";
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("No supported files found in selected folder.", "No Files Found", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }



        private void CreateFeatureChain(TextBox jiraTextBox, TextBox descTextBox, TextBox versionTextBox, DataGridView projectsGrid)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(jiraTextBox.Text))
                {
                    MessageBox.Show("Please enter a JIRA ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(descTextBox.Text))
                {
                    MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(versionTextBox.Text))
                {
                    MessageBox.Show("Please enter a version.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Check version range (10000-30000)
                var version = versionTextBox.Text.Trim();
                if (!FeatureChainService.IsVersionInValidRange(version))
                {
                    var warningMessage = FeatureChainService.GetVersionRangeWarningMessage(version);
                    var versionResult = MessageBox.Show(warningMessage, "Version Range Warning", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (versionResult == DialogResult.No)
                        return;
                }

                // Collect selected projects and validate
                var projects = new List<FeatureChainService.ProjectConfig>();
                var warnings = new List<string>();
                var hasBinaryMode = false;
                
                foreach (DataGridViewRow row in projectsGrid.Rows)
                {
                    var includeValue = false;
                    if (row.Cells["Include"]?.Value is bool include)
                        includeValue = include;
                        
                    if (includeValue && 
                        !string.IsNullOrWhiteSpace(row.Cells["Project"]?.Value?.ToString()))
                    {
                        var projectName = row.Cells["Project"].Value.ToString();
                        var mode = row.Cells["Mode"].Value?.ToString();
                        var branch = row.Cells["Branch"].Value?.ToString();
                        var tag = row.Cells["Tag"].Value?.ToString();
                        var fork = row.Cells["Fork"].Value?.ToString();
                        
                        // Rule 1: Mode is mandatory
                        if (string.IsNullOrWhiteSpace(mode))
                        {
                            MessageBox.Show($"Project '{projectName}' must have a mode (source/binary/ignore)", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Rule 2: Cannot have both branch and tag
                        if (!string.IsNullOrWhiteSpace(branch) && !string.IsNullOrWhiteSpace(tag))
                        {
                            MessageBox.Show($"Project '{projectName}' cannot have both branch and tag - choose one", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Rule 2: Must have either branch or tag
                        if (string.IsNullOrWhiteSpace(branch) && string.IsNullOrWhiteSpace(tag))
                        {
                            MessageBox.Show($"Project '{projectName}' must have either branch or tag", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Rule 3: Feature branch should have fork (warning)
                        if (!string.IsNullOrWhiteSpace(branch) && branch.StartsWith("dev/DEPM-") && string.IsNullOrWhiteSpace(fork))
                        {
                            warnings.Add($"Project '{projectName}' uses feature branch '{branch}' but no fork specified - consider using a fork");
                        }
                        
                        // Rule 5: Track if any project uses binary mode
                        if (mode == "binary")
                        {
                            hasBinaryMode = true;
                        }
                        
                        // Rule 6: Content cannot use stage branch
                        if (projectName == "content" && branch == "stage")
                        {
                            MessageBox.Show($"Content project cannot use 'stage' branch - use 'main' or 'integration'", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Rule 7: Tests project should prefer branches (warning)
                        if (projectName == "tests" && !string.IsNullOrWhiteSpace(tag))
                        {
                            warnings.Add($"Tests project should use branches instead of tags for better test practices");
                        }
                        
                        projects.Add(new FeatureChainService.ProjectConfig
                        {
                            ProjectName = projectName,
                            Mode = mode,
                            DevMode = row.Cells["DevMode"].Value?.ToString() ?? "",
                            Branch = branch,
                            Tag = tag,
                            TestsEnabled = row.Cells["Tests"]?.Value is bool tests && tests,
                            ForkRepository = fork ?? ""
                        });
                    }
                }
                
                // Rule 5: Global version required if binary mode used
                if (hasBinaryMode && string.IsNullOrWhiteSpace(versionTextBox.Text))
                {
                    MessageBox.Show("Global version is required when at least one project uses binary mode", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Show warnings if any
                if (warnings.Any())
                {
                    var warningMessage = "Warnings found:\n\n" + string.Join("\n", warnings) + "\n\nContinue anyway?";
                    if (MessageBox.Show(warningMessage, "Validation Warnings", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
                }

                if (!projects.Any())
                {
                    MessageBox.Show("Please select at least one project.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var request = new FeatureChainService.FeatureChainRequest
                {
                    JiraId = jiraTextBox.Text.Trim(),
                    Description = descTextBox.Text.Trim(),
                    Version = versionTextBox.Text.Trim(),
                    Projects = projects
                };

                var featureService = new FeatureChainService();
                var defaultPath = @"C:\ChainFileEditor\Tests\Chains";
                var filePath = featureService.CreateFeatureChainFile(request, defaultPath);

                _statusLabel.Text = $"Feature chain file created: {Path.GetFileName(filePath)}";
                
                var result = MessageBox.Show($"Feature chain file created successfully!\n\nFile: {filePath}\n\nWould you like to open the file location?", 
                    "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating feature chain: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAllProjectBranches(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var branchService = new BranchService();
                var projects = branchService.GetAllProjectBranchStatus(chain);
                
                projectsGrid.Rows.Clear();
                foreach (var project in projects)
                {
                    projectsGrid.Rows.Add(project.ProjectName, project.CurrentBranch, project.Status, "");
                }
                
                _statusLabel.Text = $"Loaded {projects.Count} projects from file";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateAllBranches(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var projectBranches = new Dictionary<string, string>();
                
                foreach (DataGridViewRow row in projectsGrid.Rows)
                {
                    var projectName = row.Cells["Project"].Value?.ToString();
                    var newBranch = row.Cells["NewBranch"].Value?.ToString();
                    
                    if (!string.IsNullOrEmpty(projectName) && !string.IsNullOrWhiteSpace(newBranch))
                    {
                        projectBranches[projectName] = newBranch.Trim();
                    }
                }

                if (!projectBranches.Any())
                {
                    MessageBox.Show("Please enter new branch names for projects you want to update.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var chain = _parser.ParsePropertiesFile(_currentFile);
                var branchService = new BranchService();
                var updatedCount = branchService.UpdateProjectBranches(chain, projectBranches);
                
                _writer.WritePropertiesFile(_currentFile, chain);
                
                _statusLabel.Text = $"Updated {updatedCount} project branches";
                MessageBox.Show($"Branch update completed!\n\n{updatedCount} projects updated", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoadAllProjectBranches(projectsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating branches: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAllProjectModes(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var modeService = new ModeService();
                var projects = modeService.GetAllProjectModeStatus(chain);
                
                projectsGrid.Rows.Clear();
                foreach (var project in projects)
                {
                    projectsGrid.Rows.Add(project.ProjectName, project.CurrentMode, project.CurrentDevMode, "", "");
                }
                
                _statusLabel.Text = $"Loaded {projects.Count} projects from file";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateAllModes(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var projectModes = new Dictionary<string, (string mode, string devMode)>();
                
                foreach (DataGridViewRow row in projectsGrid.Rows)
                {
                    var projectName = row.Cells["Project"].Value?.ToString();
                    var newMode = row.Cells["NewMode"].Value?.ToString();
                    var newDevMode = row.Cells["NewDevMode"].Value?.ToString();
                    
                    if (!string.IsNullOrEmpty(projectName) && (!string.IsNullOrWhiteSpace(newMode) || !string.IsNullOrWhiteSpace(newDevMode)))
                    {
                        projectModes[projectName] = (newMode ?? "", newDevMode ?? "");
                    }
                }

                if (!projectModes.Any())
                {
                    MessageBox.Show("Please select new modes for projects you want to update.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var chain = _parser.ParsePropertiesFile(_currentFile);
                var modeService = new ModeService();
                var updatedCount = modeService.UpdateProjectModes(chain, projectModes);
                
                _writer.WritePropertiesFile(_currentFile, chain);
                
                _statusLabel.Text = $"Updated {updatedCount} project modes";
                MessageBox.Show($"Mode update completed!\n\n{updatedCount} projects updated", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoadAllProjectModes(projectsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating modes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAllProjectTests(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var chain = _parser.ParsePropertiesFile(_currentFile);
                var testService = new TestService();
                var projects = testService.GetAllProjectTestStatus(chain);
                
                projectsGrid.Rows.Clear();
                foreach (var project in projects)
                {
                    projectsGrid.Rows.Add(project.ProjectName, project.Status, project.TestsEnabled);
                }
                
                _statusLabel.Text = $"Loaded {projects.Count} projects from file";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateAllTests(DataGridView projectsGrid)
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var projectTests = new Dictionary<string, bool>();
                
                foreach (DataGridViewRow row in projectsGrid.Rows)
                {
                    var projectName = row.Cells["Project"].Value?.ToString();
                    var newStatus = row.Cells["NewStatus"].Value;
                    
                    if (!string.IsNullOrEmpty(projectName) && newStatus != null)
                    {
                        projectTests[projectName] = Convert.ToBoolean(newStatus);
                    }
                }

                if (!projectTests.Any())
                {
                    MessageBox.Show("No test status changes detected.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var chain = _parser.ParsePropertiesFile(_currentFile);
                var testService = new TestService();
                var updatedCount = testService.UpdateProjectTests(chain, projectTests);
                
                _writer.WritePropertiesFile(_currentFile, chain);
                
                _statusLabel.Text = $"Updated {updatedCount} project test settings";
                MessageBox.Show($"Test update completed!\n\n{updatedCount} projects updated", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoadAllProjectTests(projectsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating tests: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAvailableProjects(DataGridView projectsGrid)
        {
            try
            {
                projectsGrid.Rows.Clear();
                
                var mainRepos = new[] { "administration", "appengine", "appstudio", "consolidation", "content", "content-backup", "deployment", "dashboards", "depmservice", "designer", "framework", "modeling", "officeinteg", "olap", "olap-backup", "repository", "tests" };
                
                foreach (var repo in mainRepos)
                {
                    projectsGrid.Rows.Add(false, repo, "source", "main", "", true, "");
                }
                
                _statusLabel.Text = $"Loaded {mainRepos.Length} available projects";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string[] GetProjectBranches(string projectName)
        {
            var projectBranches = new Dictionary<string, string[]>
            {
                ["administration"] = new[] { "main", "stage", "integration", "develop", "feature/admin-ui", "hotfix/security" },
                ["appengine"] = new[] { "main", "stage", "integration", "develop", "feature/engine-v2", "performance/optimization" },
                ["appstudio"] = new[] { "main", "stage", "integration", "develop", "feature/studio-redesign", "feature/workflow" },
                ["central"] = new[] { "main", "stage", "integration", "develop", "feature/central-api", "feature/central-mt" },
                ["consolidation"] = new[] { "main", "stage", "integration", "develop", "feature/data-merge", "feature/reporting" },
                ["content"] = new[] { "main", "integration", "develop", "feature/content-mgmt", "feature/versioning" },
                ["content-backup"] = new[] { "main", "stage", "integration", "develop", "feature/backup-v2" },
                ["deployment"] = new[] { "main", "stage", "integration", "develop", "feature/auto-deploy", "feature/rollback" },
                ["dashboards"] = new[] { "main", "stage", "integration", "develop", "feature/dashboard-v3", "feature/analytics" },
                ["depmservice"] = new[] { "main", "stage", "integration", "develop", "feature/microservices", "feature/api-gateway" },
                ["designer"] = new[] { "main", "stage", "integration", "develop", "feature/designer-tools", "feature/templates" },
                ["framework"] = new[] { "main", "stage", "integration", "develop", "feature/framework-v4", "feature/plugins" },
                ["modeling"] = new[] { "main", "stage", "integration", "develop", "feature/model-builder", "feature/validation" },
                ["officeinteg"] = new[] { "main", "stage", "integration", "develop", "feature/office365", "feature/sharepoint" },
                ["olap"] = new[] { "main", "stage", "integration", "develop", "feature/olap-engine", "performance/queries" },
                ["olap-backup"] = new[] { "main", "stage", "integration", "develop", "feature/olap-backup-v2" },
                ["repository"] = new[] { "main", "stage", "integration", "develop", "feature/repo-api", "feature/versioning" },
                ["tests"] = new[] { "main", "stage", "integration", "develop", "feature/test-automation", "feature/parallel-tests" }
            };
            
            return projectBranches.ContainsKey(projectName) ? projectBranches[projectName] : new[] { "main", "stage", "integration" };
        }

        private string[] GetForkBranches(string projectName, string forkOwner)
        {
            var baseBranches = GetProjectBranches(projectName).ToList();
            baseBranches.AddRange(new[] { $"feature/{forkOwner}-changes", $"personal/{forkOwner}-dev", $"experimental/{forkOwner}" });
            return baseBranches.ToArray();
        }
        
        private string SanitizeForBranch(string input)
        {
            return input.ToLower()
                       .Replace(" ", "-")
                       .Replace("_", "-")
                       .Replace(".", "-")
                       .Replace(",", "")
                       .Replace(":", "")
                       .Replace(";", "")
                       .Replace("!", "")
                       .Replace("?", "")
                       .Replace("(", "")
                       .Replace(")", "")
                       .Replace("[", "")
                       .Replace("]", "")
                       .Replace("{", "")
                       .Replace("}", "")
                       .Replace("'", "")
                       .Replace("\"", "")
                       .Replace("/", "-")
                       .Replace("\\", "-");
        }

        private void SelectBtn_Click(object sender, EventArgs e)
        {
            var filePathLabel = this.Controls.OfType<Panel>().First().Controls.OfType<Label>().Skip(1).First();
            ShowFileSelector(filePathLabel);
        }


    }
}