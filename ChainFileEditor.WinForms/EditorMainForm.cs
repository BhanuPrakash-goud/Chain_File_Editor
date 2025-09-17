using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.WinForms
{
    public partial class EditorMainForm : Form
    {
        private readonly ChainFileParser _parser;
        private readonly ChainFileWriter _writer;
        private readonly ChainValidator _validator;
        private readonly FeatureChainService _featureService;
        private readonly RebaseService _rebaseService;
        private readonly BranchService _branchService;
        private readonly ModeService _modeService;
        private readonly TestService _testService;
        private readonly AutoFixService _autoFixService;
        
        private Panel _sidebar;
        private Panel _mainPanel;
        private Panel _statusPanel;
        private Label _statusLabel;
        private string _currentFile;
        private ChainModel _currentChain;
        private Button _activeButton;

        public EditorMainForm()
        {
            _parser = new ChainFileParser();
            _writer = new ChainFileWriter();
            var rules = ValidationRuleFactory.CreateAllRules();
            _validator = new ChainValidator(rules);
            _featureService = new FeatureChainService();
            _rebaseService = new RebaseService();
            _branchService = new BranchService();
            _modeService = new ModeService();
            _testService = new TestService();
            _autoFixService = new AutoFixService();
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "Chain File Editor";
            this.Size = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9F);

            CreateStatusPanel();
            CreateFileSelector();
            CreateMainPanel();
            CreateSidebar();
            
            ShowValidatePanel(); // Default panel

            this.ResumeLayout(false);
        }

        private void CreateFileSelector()
        {
            var filePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10, 8, 10, 8)
            };

            var fileLabel = new Label { Text = "Chain File:", Location = new Point(10, 10), Size = new Size(70, 20) };
            var fileCombo = new ComboBox { Location = new Point(85, 8), Size = new Size(600, 23), Name = "GlobalFileCombo", DropDownStyle = ComboBoxStyle.DropDown };
            var browseBtn = new Button { Text = "Browse", Location = new Point(695, 7), Size = new Size(70, 25) };
            var refreshBtn = new Button { Text = "Refresh", Location = new Point(775, 7), Size = new Size(70, 25) };
            
            fileCombo.SelectedIndexChanged += (s, e) => {
                if (fileCombo.SelectedItem != null)
                    LoadChainFile(fileCombo.SelectedItem.ToString());
            };
            fileCombo.TextChanged += (s, e) => {
                if (File.Exists(fileCombo.Text))
                    LoadChainFile(fileCombo.Text);
            };
            browseBtn.Click += (s, e) => BrowseGlobalFile();
            refreshBtn.Click += (s, e) => RefreshChainFiles();
            
            RefreshChainFiles();

            filePanel.Controls.AddRange(new Control[] { fileLabel, fileCombo, browseBtn, refreshBtn });
            this.Controls.Add(filePanel);
        }

        private void CreateSidebar()
        {
            _sidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.FromArgb(37, 55, 70)
            };

            var buttons = new[]
            {
                CreateSidebarButton("✓ Validate", Color.FromArgb(52, 152, 219), ShowValidatePanel),
                CreateSidebarButton("🏷 Rebase", Color.FromArgb(230, 126, 34), ShowRebasePanel),
                CreateSidebarButton("🔗 Feature Chain", Color.FromArgb(46, 204, 113), ShowFeaturePanel),
                CreateSidebarButton("🌿 Branch", Color.FromArgb(155, 89, 182), ShowBranchPanel),
                CreateSidebarButton("⚙ Modes", Color.FromArgb(26, 188, 156), ShowModesPanel),
                CreateSidebarButton("🧪 Tests", Color.FromArgb(231, 76, 60), ShowTestsPanel),
                CreateSidebarButton("📄 Current File", Color.FromArgb(149, 165, 166), ShowCurrentFilePanel)
            };

            int y = 20;
            foreach (var btn in buttons)
            {
                btn.Location = new Point(10, y);
                _sidebar.Controls.Add(btn);
                y += 60;
            }

            this.Controls.Add(_sidebar);
        }

        private Button CreateSidebarButton(string text, Color accentColor, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(180, 50),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                Tag = accentColor
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                SetActiveButton(btn);
                clickHandler(s, e);
            };
            return btn;
        }

        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = Color.Transparent;
            }
            _activeButton = button;
            button.BackColor = (Color)button.Tag;
        }

        private void CreateMainPanel()
        {
            _mainPanel = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(this.Width - 240, this.Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(10)
            };
            this.Controls.Add(_mainPanel);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(52, 73, 94)
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9)
            };

            _statusPanel.Controls.Add(_statusLabel);
            this.Controls.Add(_statusPanel);
        }

        private void ShowValidatePanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            var panel = new Panel { Dock = DockStyle.Fill };
            
            // Validation Options
            var optionsPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
            var validateBtn = new Button { Text = "Run Validation", Location = new Point(2, 3), Size = new Size(120, 24), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            validateBtn.Click += (s, ev) => RunValidation();
            
            var summaryBtn = new Button { Text = "Validation Summary", Location = new Point(130, 3), Size = new Size(130, 24), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            summaryBtn.Click += (s, ev) => ShowValidationSummaryInPanel();
            
            var analysisBtn = new Button { Text = "Analysis", Location = new Point(270, 3), Size = new Size(80, 24), BackColor = Color.FromArgb(155, 89, 182), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            analysisBtn.Click += (s, ev) => ShowAnalysisInPanel();
            
            var autoFixBtn = new Button { Text = "Auto Fix", Location = new Point(360, 3), Size = new Size(80, 24), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            autoFixBtn.Click += (s, ev) => RunAutoFix();
            
            optionsPanel.Controls.AddRange(new Control[] { validateBtn, summaryBtn, analysisBtn, autoFixBtn });
            
            // Project Details Grid (Top Half)
            var projectsGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "ProjectsGrid",
                BackColor = Color.White,
                GridColor = Color.FromArgb(224, 224, 224),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 15 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mode", HeaderText = "Mode", FillWeight = 10 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DevMode", HeaderText = "Dev Mode", FillWeight = 10 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Branch", HeaderText = "Branch", FillWeight = 20 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tag", HeaderText = "Tag", FillWeight = 15 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fork", HeaderText = "Fork", FillWeight = 20 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tests", HeaderText = "Tests", FillWeight = 10 });
            
            // Results Panel (Bottom Half)
            var resultsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Name = "ValidationResults",
                BackColor = Color.White
            };
            
            // Default validation grid
            var validationGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "ValidationGrid",
                BackColor = Color.White,
                GridColor = Color.FromArgb(224, 224, 224),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity", FillWeight = 12 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RuleId", HeaderText = "Rule ID", FillWeight = 15 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 15 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message", FillWeight = 35 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AutoFix", HeaderText = "Auto-Fixable", FillWeight = 10 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SuggestedFix", HeaderText = "Suggested Fix", FillWeight = 18 });
            validationGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNumber", HeaderText = "Line", FillWeight = 5 });
            
            resultsPanel.Controls.Add(validationGrid);

            panel.Controls.AddRange(new Control[] { resultsPanel, projectsGrid, optionsPanel });
            _mainPanel.Controls.Add(panel);
        }

        private void ShowRebasePanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            // Version Section
            var versionPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
            var currentLabel = new Label { Text = "Current Version:", Location = new Point(0, 8), Size = new Size(100, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var currentValue = new TextBox { Location = new Point(110, 6), Size = new Size(100, 23), ReadOnly = true, Name = "CurrentVersion" };
            var newLabel = new Label { Text = "New Version:", Location = new Point(220, 8), Size = new Size(80, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var newValue = new TextBox { Location = new Point(310, 6), Size = new Size(100, 23), Name = "NewVersion" };
            var rebaseBtn = new Button { Text = "Rebase", Location = new Point(420, 5), Size = new Size(80, 25), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            rebaseBtn.Click += (s, ev) => RunRebase(currentValue.Text, newValue.Text, false);
            
            if (_currentChain != null) currentValue.Text = _rebaseService.ExtractCurrentVersion(_currentChain);
            versionPanel.Controls.AddRange(new Control[] { currentLabel, currentValue, newLabel, newValue, rebaseBtn });
            
            // Projects Grid
            var projectsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "ProjectsGrid",
                BackColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false
            };
            projectsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Update", HeaderText = "Update", FillWeight = 10 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 30 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentVersion", HeaderText = "Current Version", FillWeight = 30 });
            projectsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PropertyType", HeaderText = "Property Type", FillWeight = 30 });
            
            if (_currentChain != null)
            {
                var projectVersions = _rebaseService.AnalyzeProjectVersions(_currentChain);
                foreach (var pv in projectVersions)
                {
                    projectsGrid.Rows.Add(true, pv.ProjectName, pv.CurrentValue, pv.PropertyType);
                }
            }
            
            _mainPanel.Controls.AddRange(new Control[] { projectsGrid, versionPanel });
        }

        private void ShowFeaturePanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            var panel = new Panel { Dock = DockStyle.Fill };
            
            // Header Section
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
            var jiraLabel = new Label { Text = "JIRA ID:", Location = new Point(0, 5), Size = new Size(60, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var jiraText = new TextBox { Location = new Point(70, 3), Size = new Size(100, 23), Name = "JiraId" };
            var descLabel = new Label { Text = "Description:", Location = new Point(180, 5), Size = new Size(80, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var descText = new TextBox { Location = new Point(270, 3), Size = new Size(200, 23), Name = "Description" };
            var versionLabel = new Label { Text = "Version:", Location = new Point(480, 5), Size = new Size(60, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var versionText = new TextBox { Location = new Point(550, 3), Size = new Size(80, 23), Text = "20013", Name = "BuildVersion" };

            var testsBtn = new Button { Text = "Integration Tests", Location = new Point(640, 2), Size = new Size(120, 25), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            testsBtn.Click += (s, ev) => ShowIntegrationTestsDialog();
            var createBtn = new Button { Text = "Create", Location = new Point(770, 2), Size = new Size(80, 25), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            createBtn.Click += (s, ev) => {
                try {
                    CreateFeatureChain(jiraText.Text, descText.Text, versionText.Text);
                } catch (Exception ex) {
                    MessageBox.Show($"Error creating feature chain: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            
            headerPanel.Controls.AddRange(new Control[] { jiraLabel, jiraText, descLabel, descText, versionLabel, versionText, testsBtn, createBtn });
            
            // Projects Grid
            var projectsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "FeatureProjectsGrid",
                BackColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            projectsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Include", HeaderText = "Include", FillWeight = 8 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 15 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Mode", HeaderText = "Mode", FillWeight = 10 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "DevMode", HeaderText = "Dev Mode", FillWeight = 10 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Branch", HeaderText = "Branch", FillWeight = 25 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Tag", HeaderText = "Tag", FillWeight = 12 });
            projectsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Fork", HeaderText = "Fork", FillWeight = 15 });
            projectsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Tests", HeaderText = "Tests", FillWeight = 5 });
            
            // Setup combo box columns
            var allProjects = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
            var projectCol = (DataGridViewComboBoxColumn)projectsGrid.Columns["Project"];
            projectCol.Items.AddRange(allProjects);
            
            var modeCol = (DataGridViewComboBoxColumn)projectsGrid.Columns["Mode"];
            modeCol.Items.AddRange(new[] { "source", "binary", "ignore" });
            
            var devModeCol = (DataGridViewComboBoxColumn)projectsGrid.Columns["DevMode"];
            devModeCol.Items.AddRange(new[] { "", "binary", "ignore" });
            
            var branchCol = (DataGridViewComboBoxColumn)projectsGrid.Columns["Branch"];
            branchCol.Items.AddRange(new[] { "main", "stage", "integration", "dev" });
            
            var tagCol = (DataGridViewComboBoxColumn)projectsGrid.Columns["Tag"];
            var versionTextBox = FindControlByName(headerPanel, "BuildVersion") as TextBox;
            var version = versionTextBox?.Text ?? "20013";
            tagCol.Items.AddRange(new[] { "", $"Build_12.25.11.{version}" });
            
            // Update tag dropdown when version changes
            versionText.TextChanged += (s, ev) => {
                try
                {
                    tagCol.Items.Clear();
                    tagCol.Items.AddRange(new[] { "", $"Build_12.25.11.{versionText.Text}" });
                }
                catch { /* Ignore version text change errors */ }
            };
            

            
            // Add all projects as rows
            for (int i = 0; i < allProjects.Length; i++)
            {
                var project = allProjects[i];
                var isOptional = project == "designer" || project == "tests";
                var defaultMode = "source";
                var defaultDevMode = (project == "designer" || project == "deployment" || project == "tests") ? "ignore" : "";
                var defaultTests = !(project == "designer" || project == "content" || project == "deployment" || project == "tests");
                
                var rowIndex = projectsGrid.Rows.Add(!isOptional, project, defaultMode, defaultDevMode, "integration", "", "", defaultTests);
                
                // Initialize fork options for this project
                UpdateFeatureForkOptions(projectsGrid, rowIndex, project);
            }
            
            // Add event handler for updating fork options and validation
            projectsGrid.CellEnter += (s, ev) => {
                try
                {
                    if (ev.ColumnIndex >= 0 && ev.RowIndex >= 0 && 
                        ev.ColumnIndex < projectsGrid.Columns.Count && 
                        ev.RowIndex < projectsGrid.Rows.Count &&
                        projectsGrid.Columns.Contains("Fork") &&
                        ev.ColumnIndex == projectsGrid.Columns["Fork"].Index)
                    {
                        var projectName = SafeGetCellValue(projectsGrid.Rows[ev.RowIndex], "Project", "");
                        if (!string.IsNullOrEmpty(projectName))
                        {
                            UpdateFeatureForkOptions(projectsGrid, ev.RowIndex, projectName);
                        }
                    }
                }
                catch { /* Ignore grid event errors */ }
            };
            
            projectsGrid.CellValueChanged += (s, ev) => {
                try
                {
                    if (ev.RowIndex < 0 || ev.RowIndex >= projectsGrid.Rows.Count || 
                        ev.ColumnIndex < 0 || ev.ColumnIndex >= projectsGrid.Columns.Count) return;
                        
                    var row = projectsGrid.Rows[ev.RowIndex];
                    if (row?.Cells == null) return;
                    
                    if (projectsGrid.Columns.Contains("Branch") && ev.ColumnIndex == projectsGrid.Columns["Branch"].Index)
                    {
                        var branchValue = SafeGetCellValue(row, "Branch", "");
                        if (!string.IsNullOrEmpty(branchValue))
                        {
                            try 
                            { 
                                if (projectsGrid.Columns.Contains("Tag"))
                                    row.Cells["Tag"].Value = ""; 
                            } 
                            catch { /* Ignore tag clearing errors */ }
                            
                            // Auto-format dev branch
                            if (branchValue == "dev")
                            {
                                try
                                {
                                    var jiraId = jiraText?.Text?.Trim() ?? "";
                                    var description = descText?.Text?.Trim() ?? "";
                                    if (!string.IsNullOrEmpty(jiraId) && !string.IsNullOrEmpty(description))
                                    {
                                        var formattedBranch = $"dev/DEPM-{jiraId}-{description.Replace(" ", "-").ToLower()}";
                                        if (projectsGrid.Columns.Contains("Branch"))
                                        {
                                            row.Cells["Branch"].Value = formattedBranch;
                                        }
                                    }
                                }
                                catch { /* Ignore dev branch formatting errors */ }
                            }
                        }
                        ValidateFeatureChainRow(projectsGrid, ev.RowIndex);
                    }
                    else if (projectsGrid.Columns.Contains("Tag") && ev.ColumnIndex == projectsGrid.Columns["Tag"].Index)
                    {
                        var tagValue = SafeGetCellValue(row, "Tag", "");
                        if (!string.IsNullOrEmpty(tagValue))
                        {
                            try 
                            { 
                                if (projectsGrid.Columns.Contains("Branch"))
                                    row.Cells["Branch"].Value = ""; 
                            } 
                            catch { /* Ignore branch clearing errors */ }
                        }
                        ValidateFeatureChainRow(projectsGrid, ev.RowIndex);
                    }
                }
                catch { /* Ignore grid event errors */ }
            };
            
            panel.Controls.AddRange(new Control[] { projectsGrid, headerPanel });
            _mainPanel.Controls.Add(panel);
        }

        private void ShowBranchPanel(object sender = null, EventArgs e = null)
        {
            try
            {
                _mainPanel.Controls.Clear();
                
                var panel = new Panel { Dock = DockStyle.Fill };
                
                // Branch Management Section
                var branchPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
                
                var updateBtn = new Button { Text = "Update Branches", Location = new Point(5, 2), Size = new Size(120, 25), BackColor = Color.FromArgb(155, 89, 182), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                updateBtn.Click += (s, ev) => UpdateAllBranches();
                
                branchPanel.Controls.Add(updateBtn);
                
                // Branch Status Grid with editable branches
                var statusGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    Name = "BranchStatusGrid",
                    BackColor = Color.White,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false
                };
                
                statusGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 40, ReadOnly = true });
                
                var branchCol = new DataGridViewComboBoxColumn { Name = "NewBranch", HeaderText = "Branch", FillWeight = 60 };
                branchCol.Items.AddRange(new[] { "", "main", "stage", "integration", "dev/feature" });
                statusGrid.Columns.Add(branchCol);
                
                if (_currentChain?.Sections != null)
                {
                    foreach (var section in _currentChain.Sections)
                    {
                        var currentBranch = section.Properties?.GetValueOrDefault("branch", "") ?? "";
                        
                        // Recommend branch for tests project if it doesn't have one
                        if (section.Name == "tests" && string.IsNullOrEmpty(currentBranch))
                        {
                            currentBranch = "integration";
                        }
                        
                        statusGrid.Rows.Add(section.Name, currentBranch);
                    }
                }
                
                panel.Controls.AddRange(new Control[] { statusGrid, branchPanel });
                _mainPanel.Controls.Add(panel);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error loading branch panel: {ex.Message}";
            }
        }

        private void ShowModesPanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            var panel = new Panel { Dock = DockStyle.Fill };
            
            // Mode Configuration Section
            var configPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
            
            var updateBtn = new Button { Text = "Update Modes", Location = new Point(5, 2), Size = new Size(120, 25), BackColor = Color.FromArgb(26, 188, 156), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            updateBtn.Click += (s, ev) => UpdateAllModes();
            
            configPanel.Controls.Add(updateBtn);
            
            // Mode Status Grid with editable modes
            var statusGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "ModeStatusGrid",
                BackColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            statusGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 40, ReadOnly = true });
            statusGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Mode", HeaderText = "Mode", FillWeight = 30 });
            statusGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "DevMode", HeaderText = "Dev Mode", FillWeight = 30 });
            
            var modeCol = (DataGridViewComboBoxColumn)statusGrid.Columns["Mode"];
            modeCol.Items.AddRange(_modeService.GetValidModes().ToArray());
            
            var devModeCol = (DataGridViewComboBoxColumn)statusGrid.Columns["DevMode"];
            devModeCol.Items.AddRange(new[] { "", "binary", "ignore", "source" });
            
            if (_currentChain != null)
            {
                foreach (var section in _currentChain.Sections)
                {
                    var currentMode = section.Properties.GetValueOrDefault("mode", "");
                    var currentDevMode = section.Properties.GetValueOrDefault("mode.devs", "");
                    statusGrid.Rows.Add(section.Name, currentMode, currentDevMode);
                }
            }
            
            panel.Controls.AddRange(new Control[] { statusGrid, configPanel });
            _mainPanel.Controls.Add(panel);
        }

        private void ShowTestsPanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            var panel = new Panel { Dock = DockStyle.Fill };
            
            // Test Configuration Section
            var configPanel = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 2, 2, 2) };
            
            var updateBtn = new Button { Text = "Update Tests", Location = new Point(5, 2), Size = new Size(120, 25), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            updateBtn.Click += (s, ev) => UpdateAllTests();
            
            configPanel.Controls.Add(updateBtn);
            
            // Test Status Grid with editable tests
            var statusGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                Name = "TestStatusGrid",
                BackColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            statusGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Project", HeaderText = "Project", FillWeight = 25, ReadOnly = true });
            statusGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "UnitTests", HeaderText = "Unit Tests", FillWeight = 20 });
            statusGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IntegrationTests", HeaderText = "Integration Tests", FillWeight = 20 });
            statusGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "TestSuite", HeaderText = "Test Suite", FillWeight = 35 });
            
            if (_currentChain != null)
            {
                foreach (var section in _currentChain.Sections)
                {
                    var unitTests = section.Properties.GetValueOrDefault("tests.unit", "false") == "true";
                    var integrationTests = GetIntegrationTestsStatus(section.Name);
                    
                    var rowIndex = statusGrid.Rows.Add(section.Name, unitTests, integrationTests, "");
                    
                    // Setup test suite dropdown for this row
                    var testSuiteCell = statusGrid.Rows[rowIndex].Cells["TestSuite"] as DataGridViewComboBoxCell;
                    if (testSuiteCell != null)
                    {
                        var availableTestSuites = GetAvailableTestSuitesArray(section.Name);
                        testSuiteCell.Items.Clear();
                        testSuiteCell.Items.Add("");
                        testSuiteCell.Items.AddRange(availableTestSuites);
                    }
                }
            }
            
            panel.Controls.AddRange(new Control[] { statusGrid, configPanel });
            _mainPanel.Controls.Add(panel);
        }

        private void ShowCurrentFilePanel(object sender = null, EventArgs e = null)
        {
            _mainPanel.Controls.Clear();
            
            var contentTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(248, 248, 248)
            };
            
            if (!string.IsNullOrEmpty(_currentFile) && File.Exists(_currentFile))
            {
                contentTextBox.Text = File.ReadAllText(_currentFile);
            }
            else
            {
                contentTextBox.Text = "No file loaded. Please select a chain file to view its content.";
            }
            
            _mainPanel.Controls.Add(contentTextBox);
        }

        // Event Handlers
        private void BrowseGlobalFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Chain files (*.properties;*.chain)|*.properties;*.chain|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var globalFileCombo = FindControlByName(this, "GlobalFileCombo") as ComboBox;
                    if (globalFileCombo != null)
                    {
                        globalFileCombo.Text = dialog.FileName;
                    }
                    LoadChainFile(dialog.FileName);
                    RefreshChainFiles();
                }
            }
        }
        
        private void RefreshChainFiles()
        {
            var globalFileCombo = FindControlByName(this, "GlobalFileCombo") as ComboBox;
            if (globalFileCombo == null) return;
            
            var currentText = globalFileCombo.Text;
            globalFileCombo.Items.Clear();
            
            var searchPaths = new[] { 
                @"C:\ChainFileEditor\Tests\Chains",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    var chainFiles = Directory.GetFiles(path, "*.properties", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(path, "*.chain", SearchOption.AllDirectories))
                        .OrderBy(f => Path.GetFileName(f));
                    
                    foreach (var file in chainFiles)
                    {
                        if (!globalFileCombo.Items.Contains(file))
                            globalFileCombo.Items.Add(file);
                    }
                }
            }
            
            globalFileCombo.Text = currentText;
        }

        private void BrowseFile(TextBox fileTextBox)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Properties files (*.properties)|*.properties|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    fileTextBox.Text = dialog.FileName;
                    LoadChainFile(dialog.FileName);
                }
            }
        }

        private void LoadChainFile(string filePath)
        {
            try
            {
                _currentFile = filePath;
                _currentChain = _parser.ParsePropertiesFile(filePath);
                _statusLabel.Text = $"Loaded: {Path.GetFileName(filePath)}";
                
                // Update project combos
                UpdateProjectCombos();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateProjectCombos()
        {
            if (_currentChain == null) return;
            
            var projects = _currentChain.Sections.Select(s => s.Name).ToArray();
            
            foreach (Control control in _mainPanel.Controls)
            {
                UpdateComboInPanel(control, "ProjectCombo", projects);
            }
        }

        private void UpdateComboInPanel(Control parent, string comboName, string[] items)
        {
            var combo = FindControlByName(parent, comboName) as ComboBox;
            if (combo != null)
            {
                combo.Items.Clear();
                combo.Items.AddRange(items);
                if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            }
        }

        private Control FindControlByName(Control parent, string name)
        {
            if (parent.Name == name) return parent;
            foreach (Control child in parent.Controls)
            {
                var found = FindControlByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void RunValidation()
        {
            if (string.IsNullOrEmpty(_currentFile) || _currentChain == null)
            {
                _statusLabel.Text = "Please load a file first";
                return;
            }

            var report = _validator.Validate(_currentChain);
            var projectsGrid = FindControlByName(_mainPanel, "ProjectsGrid") as DataGridView;
            var validationGrid = FindControlByName(_mainPanel, "ValidationGrid") as DataGridView;
            
            // Load project details
            if (projectsGrid != null)
            {
                projectsGrid.Rows.Clear();
                
                foreach (var section in _currentChain.Sections)
                {
                    var mode = section.Properties.GetValueOrDefault("mode", "");
                    var devMode = section.Properties.GetValueOrDefault("mode.devs", "");
                    var branch = section.Properties.GetValueOrDefault("branch", "");
                    var tag = section.Properties.GetValueOrDefault("tag", "");
                    var fork = section.Properties.GetValueOrDefault("fork", "");
                    var tests = section.Properties.GetValueOrDefault("tests.unit", "false");
                    
                    projectsGrid.Rows.Add(section.Name, mode, devMode, branch, tag, fork, tests);
                }
            }
            
            // Load validation results
            var resultsPanel = FindControlByName(_mainPanel, "ValidationResults") as Panel;
            var validationResultsGrid = FindControlByName(resultsPanel, "ValidationGrid") as DataGridView;
            if (validationResultsGrid != null)
            {
                validationResultsGrid.Rows.Clear();
                
                foreach (var issue in report.Issues)
                {
                    var lineNumber = GetLineNumberForSection(issue.SectionName);
                    var autoFixable = issue.IsAutoFixable ? "Yes" : "No";
                    var suggestedFix = issue.SuggestedFix ?? "";
                    validationResultsGrid.Rows.Add(issue.Severity.ToString(), issue.RuleId, issue.SectionName, issue.Message, autoFixable, suggestedFix, lineNumber);
                }
                
                // Add valid projects (projects without issues)
                var projectsWithIssues = report.Issues.Select(i => i.SectionName).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToHashSet();
                var validProjects = _currentChain.Sections.Where(s => !projectsWithIssues.Contains(s.Name)).ToList();
                
                foreach (var project in validProjects)
                {
                    var lineNumber = GetLineNumberForSection(project.Name);
                    validationResultsGrid.Rows.Add("Valid", "N/A", project.Name, "All validation rules passed", "No", "", lineNumber);
                }
            }
            
            var errors = report.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            var warnings = report.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
            var fixableCount = report.Issues.Count(i => i.IsAutoFixable);
            var validCount = _currentChain.Sections.Count - report.Issues.Select(i => i.SectionName).Where(s => !string.IsNullOrEmpty(s)).Distinct().Count();
            _statusLabel.Text = $"Loaded {_currentChain.Sections.Count} projects - Validation: {errors} errors, {warnings} warnings, {fixableCount} auto-fixable, {validCount} valid";
        }

        private void ShowValidationSummaryInPanel()
        {
            if (_currentChain == null)
            {
                MessageBox.Show("Please load a chain file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resultsPanel = FindControlByName(_mainPanel, "ValidationResults") as Panel;
            if (resultsPanel == null) return;

            resultsPanel.Controls.Clear();
            
            var report = _validator.Validate(_currentChain);
            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warnings = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
            var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();
            var totalProjects = _currentChain.Sections.Count;
            var projectsWithIssues = report.Issues.Select(i => i.SectionName).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            var validProjects = _currentChain.Sections.Where(s => !projectsWithIssues.Contains(s.Name)).ToList();

            var summaryText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White
            };

            var summary = new System.Text.StringBuilder();
            summary.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            summary.AppendLine("                           VALIDATION SUMMARY REPORT");
            summary.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            summary.AppendLine();
            
            summary.AppendLine($"FILE: {Path.GetFileName(_currentFile)}");
            summary.AppendLine($"DATE: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine();
            
            summary.AppendLine($"VALIDATION STATISTICS:");
            summary.AppendLine($"  Total Projects: {totalProjects}");
            summary.AppendLine($"  Valid Projects: {validProjects.Count}");
            summary.AppendLine($"  Invalid Projects: {projectsWithIssues.Count}");
            summary.AppendLine($"  Total Issues: {report.Issues.Count}");
            summary.AppendLine($"  ❌ Errors: {errors.Count}");
            summary.AppendLine($"  ⚠ Warnings: {warnings.Count}");
            summary.AppendLine($"  ⚙ Auto-fixable: {fixableIssues.Count}");
            summary.AppendLine($"  Overall Status: {(errors.Count == 0 ? "✓ PASSED" : "❌ FAILED")}");
            summary.AppendLine();

            if (errors.Count > 0)
            {
                summary.AppendLine($"ERRORS ({errors.Count}):");
                foreach (var error in errors.OrderBy(e => e.SectionName).ThenBy(e => e.RuleId))
                {
                    summary.AppendLine($"  ❌ {error.SectionName ?? "Global"}: {error.Message}");
                    if (error.IsAutoFixable)
                        summary.AppendLine($"     ✓ Auto-fix available");
                }
                summary.AppendLine();
            }
            
            if (warnings.Count > 0)
            {
                summary.AppendLine($"WARNINGS ({warnings.Count}):");
                foreach (var warning in warnings.OrderBy(w => w.SectionName).ThenBy(w => w.RuleId))
                {
                    summary.AppendLine($"  ⚠ {warning.SectionName ?? "Global"}: {warning.Message}");
                    if (warning.IsAutoFixable)
                        summary.AppendLine($"     ✓ Auto-fix available");
                }
                summary.AppendLine();
            }

            if (validProjects.Count > 0)
            {
                summary.AppendLine($"VALID PROJECTS ({validProjects.Count}):");
                foreach (var project in validProjects.OrderBy(p => p.Name))
                {
                    summary.AppendLine($"  ✓ {project.Name}: All validation rules passed");
                }
            }

            summaryText.Text = summary.ToString();
            resultsPanel.Controls.Add(summaryText);
        }

        private void ShowAnalysisInPanel()
        {
            if (_currentChain == null)
            {
                MessageBox.Show("Please load a chain file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resultsPanel = FindControlByName(_mainPanel, "ValidationResults") as Panel;
            if (resultsPanel == null) return;

            resultsPanel.Controls.Clear();
            
            var analysisText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White
            };

            var analysis = GenerateAnalysis(_currentChain);
            analysisText.Text = analysis;
            resultsPanel.Controls.Add(analysisText);
        }
        
        private int GetLineNumberForSection(string sectionName)
        {
            if (string.IsNullOrEmpty(_currentFile) || !File.Exists(_currentFile)) return 0;
            
            var lines = File.ReadAllLines(_currentFile);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith($"{sectionName}."))
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private void RunRebase(string currentVersion, string newVersion, bool dryRun)
        {
            if (_currentChain == null || string.IsNullOrEmpty(newVersion))
            {
                _statusLabel.Text = "Please load a file and enter new version";
                return;
            }

            // Check version range
            if (int.TryParse(newVersion, out var versionNumber))
            {
                const int minVersion = 10000;
                const int maxVersion = 39999;
                
                if (versionNumber < minVersion || versionNumber > maxVersion)
                {
                    var result = MessageBox.Show(
                        $"Warning: Version {newVersion} is outside the valid range ({minVersion}-{maxVersion}).\n\nDo you want to continue?",
                        "Version Range Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        _statusLabel.Text = "Rebase cancelled - version out of range";
                        return;
                    }
                }
            }

            var grid = FindControlByName(_mainPanel, "ProjectsGrid") as DataGridView;
            
            var selectedProjects = new List<string>();
            if (grid?.Rows != null)
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    try
                    {
                        if (row?.Cells == null) continue;
                        
                        var updateValue = false;
                        try
                        {
                            var updateCell = row.Cells["Update"];
                            if (updateCell?.Value is bool update)
                                updateValue = update;
                        }
                        catch { /* Ignore cell access errors */ }
                            
                        if (updateValue)
                        {
                            var project = SafeGetCellValue(row, "Project", "");
                            if (!string.IsNullOrEmpty(project))
                                selectedProjects.Add(project);
                        }
                    }
                    catch { continue; }
                }
            }

            var count = _rebaseService.UpdateSelectedProjects(_currentChain, newVersion, selectedProjects);
            _writer.WritePropertiesFile(_currentFile, _currentChain);
            
            // Refresh the grid with updated data
            if (grid != null)
            {
                var projectVersions = _rebaseService.AnalyzeProjectVersions(_currentChain);
                grid.Rows.Clear();
                foreach (var pv in projectVersions)
                {
                    grid.Rows.Add(true, pv.ProjectName, pv.CurrentValue, pv.PropertyType);
                }
            }
            
            // Update current version display
            var currentVersionTextBox = FindControlByName(_mainPanel, "CurrentVersion") as TextBox;
            if (currentVersionTextBox != null)
            {
                currentVersionTextBox.Text = _rebaseService.ExtractCurrentVersion(_currentChain);
            }
            
            _statusLabel.Text = $"Rebase complete: {count} projects updated to {newVersion}";
        }

        private void CreateFeatureChain(string jiraId, string description, string version)
        {
            if (string.IsNullOrEmpty(jiraId) || string.IsNullOrEmpty(description))
            {
                _statusLabel.Text = "Please enter JIRA ID and Description";
                return;
            }

            try
            {
                var grid = FindControlByName(_mainPanel, "FeatureProjectsGrid") as DataGridView;
                var projects = new List<FeatureChainService.ProjectConfig>();
                var warnings = new List<string>();
                
                if (grid?.Rows != null)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        try
                        {
                            if (row?.Cells == null) continue;
                            
                            var includeValue = false;
                            try
                            {
                                var includeCell = row.Cells["Include"];
                                if (includeCell?.Value is bool includeCheck)
                                    includeValue = includeCheck;
                            }
                            catch { /* Ignore cell access errors */ }
                            
                            if (includeValue)
                            {
                                var projectName = SafeGetCellValue(row, "Project", "");
                                var mode = SafeGetCellValue(row, "Mode", "source");
                                var devMode = SafeGetCellValue(row, "DevMode", "");
                                var branch = SafeGetCellValue(row, "Branch", "");
                                var tag = SafeGetCellValue(row, "Tag", "");
                                var forkRepo = SafeGetCellValue(row, "Fork", "");
                                
                                var testsEnabled = false;
                                try
                                {
                                    var testsCell = row.Cells["Tests"];
                                    if (testsCell?.Value is bool testsCheck)
                                        testsEnabled = testsCheck;
                                }
                                catch { /* Ignore cell access errors */ }
                                
                                // Validation warnings
                                if (!string.IsNullOrEmpty(branch) && !string.IsNullOrEmpty(tag))
                                    warnings.Add($"{projectName}: Cannot have both branch and tag");
                                
                                if (branch != null && (branch.StartsWith("dev/DEPM-") || branch == "dev/feature") && string.IsNullOrEmpty(forkRepo))
                                    warnings.Add($"{projectName}: dev/ branch requires a fork");
                                
                                if (mode == "binary" && string.IsNullOrEmpty(version))
                                    warnings.Add($"{projectName}: Binary mode requires a version to be specified");
                                
                                if (devMode == "binary" && string.IsNullOrEmpty(version))
                                    warnings.Add($"{projectName}: Binary dev mode requires a version to be specified");
                                
                                var project = new FeatureChainService.ProjectConfig
                                {
                                    ProjectName = projectName,
                                    Mode = mode,
                                    DevMode = devMode,
                                    Branch = branch,
                                    Tag = tag,
                                    ForkRepository = forkRepo,
                                    TestsEnabled = testsEnabled
                                };
                                projects.Add(project);
                            }
                        }
                        catch (Exception ex)
                        {
                            _statusLabel.Text = $"Error processing row: {ex.Message}";
                            continue;
                        }
                    }
                }
                
                // Show warnings if any
                if (warnings.Count > 0)
                {
                    var result = MessageBox.Show(
                        $"Warnings found:\n{string.Join("\n", warnings.Distinct())}\n\nDo you want to continue?",
                        "Feature Chain Warnings",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        _statusLabel.Text = "Feature chain creation cancelled";
                        return;
                    }
                }

                var request = new FeatureChainService.FeatureChainRequest
                {
                    JiraId = jiraId,
                    Description = description,
                    Version = version,
                    Projects = projects,
                    EnabledIntegrationTests = _selectedIntegrationTests
                };

                var outputPath = @"C:\ChainFileEditor\Tests\Chains";
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
                var createdFile = _featureService.CreateFeatureChainFile(request, outputPath);
                
                _statusLabel.Text = $"Feature chain created: {Path.GetFileName(createdFile)} in {outputPath}";
                MessageBox.Show($"Feature chain file created successfully:\n{createdFile}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadChainFile(createdFile);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateAllBranches()
        {
            try
            {
                if (_currentChain?.Sections == null) 
                {
                    _statusLabel.Text = "No chain file loaded";
                    return;
                }
                
                var grid = FindControlByName(_mainPanel, "BranchStatusGrid") as DataGridView;
                if (grid?.Rows == null) 
                {
                    _statusLabel.Text = "Branch grid not found";
                    return;
                }
                
                var updates = new Dictionary<string, string>();
                var warnings = new List<string>();
                
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row?.Cells == null) continue;
                    
                    var project = row.Cells["Project"]?.Value?.ToString();
                    var newBranch = row.Cells["NewBranch"]?.Value?.ToString();
                    
                    if (!string.IsNullOrEmpty(project) && !string.IsNullOrEmpty(newBranch))
                    {
                        var section = _currentChain.Sections.FirstOrDefault(s => s.Name == project);
                        if (section?.Properties != null && !string.IsNullOrEmpty(section.Properties.GetValueOrDefault("tag", "")))
                        {
                            warnings.Add($"{project} has a tag - setting branch will override it");
                        }
                        updates[project] = newBranch;
                    }
                }
                
                if (warnings.Count > 0)
                {
                    var result = MessageBox.Show(
                        $"Warning:\n{string.Join("\n", warnings)}\n\nDo you want to continue?",
                        "Tag Conflict Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        _statusLabel.Text = "Branch update cancelled";
                        return;
                    }
                }
                
                if (updates.Count > 0)
                {
                    var count = _branchService.UpdateProjectBranches(_currentChain, updates);
                    _writer.WritePropertiesFile(_currentFile, _currentChain);
                    _statusLabel.Text = $"Updated branches for {count} projects";
                    
                    // Refresh grid
                    ShowBranchPanel();
                }
                else
                {
                    _statusLabel.Text = "No branch updates to apply";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error updating branches: {ex.Message}";
            }
        }

        private void UpdateAllModes()
        {
            try
            {
                if (_currentChain == null) return;
                
                var grid = FindControlByName(_mainPanel, "ModeStatusGrid") as DataGridView;
                if (grid?.Rows == null) return;
                
                var updates = new Dictionary<string, (string mode, string devMode)>();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    try
                    {
                        if (row?.Cells == null) continue;
                        
                        var project = SafeGetCellValue(row, "Project", "");
                        var mode = SafeGetCellValue(row, "Mode", "");
                        var devMode = SafeGetCellValue(row, "DevMode", "");
                        
                        if (!string.IsNullOrEmpty(project) && !string.IsNullOrEmpty(mode))
                        {
                            updates[project] = (mode, devMode);
                        }
                    }
                    catch { continue; }
                }
                
                if (updates.Count > 0)
                {
                    var count = _modeService.UpdateProjectModes(_currentChain, updates);
                    _writer.WritePropertiesFile(_currentFile, _currentChain);
                    _statusLabel.Text = $"Updated modes for {count} projects";
                    
                    // Refresh grid
                    ShowModesPanel();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error updating modes: {ex.Message}";
            }
        }

        private void ApplyTests(string project, bool enabled)
        {
            if (_currentChain == null || string.IsNullOrEmpty(project)) return;
            
            var updates = new Dictionary<string, bool> { [project] = enabled };
            var count = _testService.UpdateProjectTests(_currentChain, updates);
            
            if (count > 0)
            {
                _writer.WritePropertiesFile(_currentFile, _currentChain);
                _statusLabel.Text = $"Unit tests {(enabled ? "enabled" : "disabled")} for {project}";
            }
        }

        private void BrowseFolder(TextBox folderTextBox)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Repository Directory";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private bool GetIntegrationTestsStatus(string projectName)
        {
            if (_currentChain?.IntegrationTests == null) return false;
            
            // Check if any integration test for this project is enabled
            // This is a simplified check - actual implementation would depend on IntegrationTestsSection structure
            return false;
        }
        
        private string[] GetAvailableTestSuitesArray(string projectName)
        {
            var testSuites = new Dictionary<string, string[]>
            {
                ["appengine"] = new[] { "AppEngineService" },
                ["administration"] = new[] { "AdministrationService" },
                ["appstudio"] = new[] { "AppStudioService" },
                ["consolidation"] = new[] { "ConsolidationService" },
                ["dashboards"] = new[] { "DashboardsService" },
                ["modeling"] = new[] { "BusinessModelingServiceSet1", "BusinessModelingServiceSet2", "ModelingService" },
                ["olap"] = new[] { "OlapService", "OlapAPI" },
                ["officeinteg"] = new[] { "OfficeIntegrationService" },
                ["content"] = new[] { "ContentIntegration" }
            };
            
            return testSuites.ContainsKey(projectName) ? testSuites[projectName] : new string[0];
        }
        
        private void UpdateAllTests()
        {
            try
            {
                if (_currentChain == null) return;
                
                var grid = FindControlByName(_mainPanel, "TestStatusGrid") as DataGridView;
                if (grid?.Rows == null) return;
                
                var unitTestUpdates = new Dictionary<string, bool>();
                var integrationTestUpdates = new Dictionary<string, bool>();
                
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row?.Cells == null) continue;
                    
                    var project = row.Cells["Project"]?.Value?.ToString();
                    if (string.IsNullOrEmpty(project)) continue;
                    
                    var unitTests = false;
                    var integrationTests = false;
                    
                    if (row.Cells["UnitTests"]?.Value is bool unitTestValue)
                        unitTests = unitTestValue;
                    
                    if (row.Cells["IntegrationTests"]?.Value is bool integrationTestValue)
                        integrationTests = integrationTestValue;
                    
                    unitTestUpdates[project] = unitTests;
                    integrationTestUpdates[project] = integrationTests;
                }
                
                if (unitTestUpdates.Count > 0)
                {
                    var count = _testService.UpdateProjectTests(_currentChain, unitTestUpdates);
                    UpdateIntegrationTests(integrationTestUpdates);
                    _writer.WritePropertiesFile(_currentFile, _currentChain);
                    _statusLabel.Text = $"Updated tests for {count} projects";
                    
                    // Refresh grid
                    ShowTestsPanel();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error updating tests: {ex.Message}";
            }
        }
        
        private void UpdateIntegrationTests(Dictionary<string, bool> integrationTestUpdates)
        {
            // This would update integration test properties in the chain file
            // Implementation depends on how integration tests are stored in the model
        }
        
        private Dictionary<string, string[]> GetKnownForks()
        {
            return new Dictionary<string, string[]>
            {
                ["dasa.petrezselyova"] = new[] { "dashboards", "modeling" },
                ["ivan.rebo"] = new[] { "administration", "appengine", "appstudio", "depmservice", "consolidation", "dashboards", "framework", "modeling", "officeinteg", "olap", "repository" },
                ["oliver.schmidt"] = new[] { "framework", "olap" },
                ["petr.novacek"] = new[] { "administration", "appengine", "appstudio", "depmservice", "consolidation", "dashboards", "deployment", "framework", "modeling", "officeinteg", "olap", "repository" },
                ["stefan.kiel"] = new[] { "framework", "olap" },
                ["vit.holy"] = new[] { "appstudio", "framework", "officeinteg" },
                ["vojtech.lahoda"] = new[] { "appengine", "consolidation", "depmservice", "framework", "modeling" }
            };
        }

        private bool HasForkForProject(string forkName, string projectName)
        {
            var knownForks = GetKnownForks();
            return knownForks.ContainsKey(forkName) && knownForks[forkName].Contains(projectName);
        }
        
        private void UpdateFeatureForkOptions(DataGridView grid, int rowIndex, string projectName)
        {
            try
            {
                if (grid?.Rows == null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
                    return;
                    
                var row = grid.Rows[rowIndex];
                if (row?.Cells == null)
                    return;
                    
                var forkCell = row.Cells["Fork"] as DataGridViewComboBoxCell;
                if (forkCell != null)
                {
                    var knownForks = GetKnownForks();
                    var availableForks = new List<string> { "" };

                    // Add forks that support this project in owner/project format
                    foreach (var fork in knownForks)
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
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error updating fork options: {ex.Message}";
            }
        }



        private List<string> _selectedIntegrationTests = new List<string>();
        
        private string SafeGetCellValue(DataGridViewRow row, string columnName, string defaultValue)
        {
            try
            {
                if (row?.Cells == null)
                    return defaultValue;
                    
                var cell = row.Cells[columnName];
                return cell?.Value?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        private void ShowIntegrationTestsDialog()
        {
            var dialog = new Form
            {
                Text = "Integration Tests Configuration",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var testsGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 400,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                BackColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            testsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Enable", FillWeight = 15 });
            testsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TestName", HeaderText = "Test Suite", FillWeight = 85 });
            
            var integrationTests = new[]
            {
                "AdhocWidgetSet1", "AdhocWidgetSet2", "AdministrationService", "AppEngineService",
                "AppsProvisioning", "AppStudioService", "BusinessModelingServiceSet1", "BusinessModelingServiceSet2",
                "BusinessModelingServiceSet3", "ConsolidationService", "DashboardsService", "dEPMAppsUpdate",
                "FarmCreation", "FarmUpgrade", "OfficeIntegrationService", "OlapService", "OlapAPI",
                "ContentIntegration", "dEPMRegressionSet1", "dEPMRegressionSet2", "dEPMRegressionSet3",
                "dEPMRegressionSet4", "SelfService", "WorkforceBudgetingSet1", "WorkforceBudgetingSet2",
                "WorkforceBudgetingSet4", "WorkforceBudgetingSet5", "MultiFarm", "EPMWorkflow",
                "ModelingService", "ModelingUI", "RelationalModeling", "FinancialReportingSet1", "FinancialReportingSet2"
            };
            
            foreach (var test in integrationTests)
            {
                var isSelected = _selectedIntegrationTests.Contains(test);
                testsGrid.Rows.Add(isSelected, test);
            }
            
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var okButton = new Button { Text = "OK", Location = new Point(10, 8), Size = new Size(75, 25) };
            var cancelButton = new Button { Text = "Cancel", Location = new Point(95, 8), Size = new Size(75, 25) };
            
            okButton.Click += (s, e) => {
                _selectedIntegrationTests.Clear();
                foreach (DataGridViewRow row in testsGrid.Rows)
                {
                    if (row.Cells["Enabled"]?.Value is bool enabled && enabled)
                    {
                        _selectedIntegrationTests.Add(row.Cells["TestName"].Value?.ToString() ?? "");
                    }
                }
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            
            cancelButton.Click += (s, e) => {
                dialog.DialogResult = DialogResult.Cancel;
                dialog.Close();
            };
            
            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton });
            dialog.Controls.AddRange(new Control[] { testsGrid, buttonPanel });
            dialog.ShowDialog();
        }

        private void ValidateFeatureChainRow(DataGridView grid, int rowIndex)
        {
            try
            {
                if (grid?.Rows == null || rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
                
                var row = grid.Rows[rowIndex];
                if (row?.Cells == null) return;
                
                var project = row.Cells["Project"]?.Value?.ToString() ?? "";
                var branch = row.Cells["Branch"]?.Value?.ToString() ?? "";
                var tag = row.Cells["Tag"]?.Value?.ToString() ?? "";
                var hasFork = !string.IsNullOrWhiteSpace(row.Cells["Fork"]?.Value?.ToString());
                
                var hasBranch = !string.IsNullOrWhiteSpace(branch);
                var hasTag = !string.IsNullOrWhiteSpace(tag);
                
                // Reset row color
                row.DefaultCellStyle.BackColor = Color.White;
                
                if (hasBranch && hasTag)
                {
                    // Both branch and tag - show error
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    _statusLabel.Text = $"Error: {project} cannot have both branch and tag";
                }
                else if (branch != null && (branch.StartsWith("dev/DEPM-") || branch == "dev/feature") && !hasFork)
                {
                    // dev/DEPM- or dev/feature branch without fork - show error
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    _statusLabel.Text = $"Error: {project} with dev/ branch must have a fork";
                }
                else if (branch == "stage")
                {
                    // Stage branch not allowed in feature chains
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    _statusLabel.Text = $"Error: {project} cannot use stage branch in feature chains";
                }
                else
                {
                    // Valid configuration
                    _statusLabel.Text = "Ready";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error validating row: {ex.Message}";
            }
        }

        private void ShowValidationSummary()
        {
            if (_currentChain == null)
            {
                MessageBox.Show("Please load a chain file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var report = _validator.Validate(_currentChain);
            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warnings = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
            var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();
            var fixableCount = fixableIssues.Count;
            var totalProjects = _currentChain.Sections.Count;
            var projectsWithIssues = report.Issues.Select(i => i.SectionName).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            var validProjects = _currentChain.Sections.Where(s => !projectsWithIssues.Contains(s.Name)).ToList();

            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"═══════════════════════════════════════════════════════════════════════════════");
            summary.AppendLine($"                           VALIDATION SUMMARY REPORT");
            summary.AppendLine($"═══════════════════════════════════════════════════════════════════════════════");
            summary.AppendLine();
            
            // FILE DETAILS
            summary.AppendLine($"FILE INFORMATION:");
            summary.AppendLine($"  File Name: {Path.GetFileName(_currentFile)}");
            summary.AppendLine($"  Full Path: {_currentFile}");
            summary.AppendLine($"  File Size: {new FileInfo(_currentFile).Length:N0} bytes");
            summary.AppendLine($"  Last Modified: {File.GetLastWriteTime(_currentFile):yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"  Created: {File.GetCreationTime(_currentFile):yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"  Lines: {File.ReadAllLines(_currentFile).Length:N0}");
            summary.AppendLine();
            
            // GLOBAL CONFIGURATION
            summary.AppendLine($"GLOBAL CONFIGURATION:");
            if (_currentChain.Global != null)
            {
                summary.AppendLine($"  Version Binary: {_currentChain.Global.VersionBinary ?? "Not set"}");
                summary.AppendLine($"  Dev Version Binary: {_currentChain.Global.DevVersionBinary ?? "Not set"}");
                summary.AppendLine($"  Description: {_currentChain.Global.Description ?? "Not set"}");
                summary.AppendLine($"  JIRA ID: {_currentChain.Global.JiraId ?? "Not set"}");
                summary.AppendLine($"  Created Date: {_currentChain.Global.CreatedDate ?? "Not set"}");
            }
            else
            {
                summary.AppendLine($"  No global configuration found");
            }
            summary.AppendLine();
            
            // VALIDATION STATISTICS
            summary.AppendLine($"VALIDATION STATISTICS:");
            summary.AppendLine($"  Total Projects: {totalProjects}");
            summary.AppendLine($"  Valid Projects: {validProjects.Count}");
            summary.AppendLine($"  Invalid Projects: {projectsWithIssues.Count}");
            summary.AppendLine($"  Total Issues: {report.Issues.Count}");
            summary.AppendLine($"  ❌ Errors: {errors.Count}");
            summary.AppendLine($"  ⚠ Warnings: {warnings.Count}");
            summary.AppendLine($"  ⚙ Auto-fixable: {fixableCount}");
            summary.AppendLine($"  Overall Status: {(errors.Count == 0 ? "✓ PASSED" : "❌ FAILED")}");
            summary.AppendLine();

            // DETAILED VALIDATION RESULTS
            if (errors.Count > 0)
            {
                summary.AppendLine($"DETAILED ERROR ANALYSIS ({errors.Count}):");
                foreach (var error in errors.OrderBy(e => e.SectionName).ThenBy(e => e.RuleId))
                {
                    summary.AppendLine($"  ❌ Rule: {error.RuleId}");
                    summary.AppendLine($"     Project: {error.SectionName ?? "Global"}");
                    summary.AppendLine($"     Message: {error.Message}");
                    summary.AppendLine($"     Auto-fixable: {(error.IsAutoFixable ? "Yes" : "No")}");
                    if (!string.IsNullOrEmpty(error.SuggestedFix))
                        summary.AppendLine($"     Suggested Fix: {error.SuggestedFix}");
                    summary.AppendLine();
                }
            }
            
            if (warnings.Count > 0)
            {
                summary.AppendLine($"DETAILED WARNING ANALYSIS ({warnings.Count}):");
                foreach (var warning in warnings.OrderBy(w => w.SectionName).ThenBy(w => w.RuleId))
                {
                    summary.AppendLine($"  ⚠ Rule: {warning.RuleId}");
                    summary.AppendLine($"     Project: {warning.SectionName ?? "Global"}");
                    summary.AppendLine($"     Message: {warning.Message}");
                    summary.AppendLine($"     Auto-fixable: {(warning.IsAutoFixable ? "Yes" : "No")}");
                    if (!string.IsNullOrEmpty(warning.SuggestedFix))
                        summary.AppendLine($"     Suggested Fix: {warning.SuggestedFix}");
                    summary.AppendLine();
                }
            }

            if (validProjects.Count > 0)
            {
                summary.AppendLine($"VALID PROJECTS DETAILS ({validProjects.Count}):");
                foreach (var project in validProjects.OrderBy(p => p.Name))
                {
                    summary.AppendLine($"  ✓ {project.Name}:");
                    summary.AppendLine($"     Mode: {project.Properties.GetValueOrDefault("mode", "not set")}");
                    summary.AppendLine($"     Dev Mode: {project.Properties.GetValueOrDefault("mode.devs", "not set")}");
                    summary.AppendLine($"     Branch: {project.Properties.GetValueOrDefault("branch", "not set")}");
                    summary.AppendLine($"     Tag: {project.Properties.GetValueOrDefault("tag", "not set")}");
                    summary.AppendLine($"     Fork: {project.Properties.GetValueOrDefault("fork", "not set")}");
                    summary.AppendLine($"     Unit Tests: {project.Properties.GetValueOrDefault("tests.unit", "false")}");
                    summary.AppendLine();
                }
            }

            var summaryForm = new Form
            {
                Text = "Detailed Validation Summary",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            var summaryText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                Text = summary.ToString()
            };

            summaryForm.Controls.Add(summaryText);
            summaryForm.ShowDialog();
        }

        private void ShowAnalysisPanel()
        {
            if (_currentChain == null)
            {
                MessageBox.Show("Please load a chain file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var analysisForm = new Form
            {
                Text = "Chain Analysis",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var analyzeBtn = new Button { Text = "Re-analyze", Location = new Point(10, 8), Size = new Size(100, 25), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            analyzeBtn.Click += (s, e) => { var newAnalysis = GenerateAnalysis(_currentChain); ((TextBox)analysisForm.Controls[0]).Text = newAnalysis; };
            buttonPanel.Controls.Add(analyzeBtn);

            var analysisText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            var analysis = GenerateAnalysis(_currentChain);
            analysisText.Text = analysis;

            analysisForm.Controls.AddRange(new Control[] { analysisText, buttonPanel });
            analysisForm.ShowDialog();
        }

        private string GenerateAnalysis(ChainModel chain)
        {
            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            analysis.AppendLine("                              CHAIN ANALYSIS REPORT");
            analysis.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            analysis.AppendLine();

            // Validation Issues Analysis
            var report = _validator.Validate(chain);
            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warnings = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
            var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();
            
            // Debug: Log fixable issues count
            System.Diagnostics.Debug.WriteLine($"Total issues: {report.Issues.Count}, Auto-fixable: {fixableIssues.Count}");

            analysis.AppendLine("DETAILED ISSUE ANALYSIS:");
            analysis.AppendLine($"  Total Issues: {report.Issues.Count}");
            analysis.AppendLine($"  Errors: {errors.Count}");
            analysis.AppendLine($"  Warnings: {warnings.Count}");
            analysis.AppendLine($"  Auto-fixable: {fixableIssues.Count}");
            analysis.AppendLine();

            if (errors.Count > 0)
            {
                analysis.AppendLine("ERRORS ANALYSIS:");
                foreach (var error in errors)
                {
                    analysis.AppendLine($"  ❌ [{error.SectionName ?? "Global"}] {error.Message}");
                    if (error.IsAutoFixable)
                        analysis.AppendLine($"     ✓ Auto-fix available: {error.SuggestedFix}");
                    else
                        analysis.AppendLine($"     ⚠ Manual fix required");
                }
                analysis.AppendLine();
            }

            if (warnings.Count > 0)
            {
                analysis.AppendLine("WARNINGS ANALYSIS:");
                foreach (var warning in warnings)
                {
                    analysis.AppendLine($"  ⚠ [{warning.SectionName ?? "Global"}] {warning.Message}");
                    if (warning.IsAutoFixable)
                        analysis.AppendLine($"     ✓ Auto-fix available: {warning.SuggestedFix}");
                    else
                        analysis.AppendLine($"     ⚠ Manual review recommended");
                }
                analysis.AppendLine();
            }

            // Configuration Pattern Analysis
            analysis.AppendLine("CONFIGURATION PATTERN ANALYSIS:");
            var modeStats = new Dictionary<string, int>();
            var branchStats = new Dictionary<string, int>();
            var testStats = new Dictionary<string, int>();
            var riskFactors = new List<string>();

            foreach (var section in chain.Sections)
            {
                var mode = section.Properties.GetValueOrDefault("mode", "unknown");
                var branch = section.Properties.GetValueOrDefault("branch", "none");
                var tests = section.Properties.GetValueOrDefault("tests.unit", "false");

                modeStats[mode] = modeStats.GetValueOrDefault(mode, 0) + 1;
                branchStats[branch] = branchStats.GetValueOrDefault(branch, 0) + 1;
                testStats[tests] = testStats.GetValueOrDefault(tests, 0) + 1;
            }

            analysis.AppendLine("  Mode Distribution:");
            foreach (var stat in modeStats.OrderByDescending(x => x.Value))
                analysis.AppendLine($"    {stat.Key}: {stat.Value} projects");

            analysis.AppendLine("  Branch Distribution:");
            foreach (var stat in branchStats.OrderByDescending(x => x.Value))
                analysis.AppendLine($"    {stat.Key}: {stat.Value} projects");

            // Risk Assessment
            var binaryProjects = chain.Sections.Count(s => s.Properties.GetValueOrDefault("mode", "") == "binary");
            if (binaryProjects > chain.Sections.Count * 0.5)
                riskFactors.Add($"High binary dependency: {binaryProjects}/{chain.Sections.Count} projects");

            var devBranches = chain.Sections.Count(s => s.Properties.GetValueOrDefault("branch", "").StartsWith("dev/"));
            if (devBranches > 0)
                riskFactors.Add($"Development branches: {devBranches} projects using dev/ branches");

            analysis.AppendLine();
            analysis.AppendLine("RISK ASSESSMENT:");
            if (riskFactors.Count == 0)
                analysis.AppendLine("  ✓ No significant risks detected");
            else
                foreach (var risk in riskFactors)
                    analysis.AppendLine($"  ⚠ {risk}");

            analysis.AppendLine();
            analysis.AppendLine("ACTIONABLE RECOMMENDATIONS:");
            if (fixableIssues.Count > 0)
                analysis.AppendLine($"  ⚙ {fixableIssues.Count} issues can be auto-fixed - click 'Auto Fix' button");
            else
                analysis.AppendLine("  ✓ No auto-fixable issues found");

            return analysis.ToString();
        }

        private void RunAutoFix()
        {
            if (_currentChain == null)
            {
                MessageBox.Show("Please load a chain file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get initial validation report
                var initialReport = _validator.Validate(_currentChain);
                var initialErrors = initialReport.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                var fixableErrors = initialErrors.Where(i => i.IsAutoFixable).ToList();
                
                if (fixableErrors.Count == 0)
                {
                    MessageBox.Show("No auto-fixable errors found.", "Auto Fix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Apply fixes
                var fixedCount = _autoFixService.ApplyAutoFixes(_currentChain, fixableErrors);
                
                // Save the file
                _writer.WritePropertiesFile(_currentFile, _currentChain);
                
                // Reload and validate
                _currentChain = _parser.ParsePropertiesFile(_currentFile);
                var finalReport = _validator.Validate(_currentChain);
                var remainingErrors = finalReport.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                
                MessageBox.Show(
                    $"Auto-fix completed!\n\n" +
                    $"Fixed {fixedCount} issues\n" +
                    $"Remaining errors: {remainingErrors}\n" +
                    (remainingErrors == 0 ? "\n✓ Chain file is now valid!" : "\nSome errors may require manual attention."),
                    "Auto Fix Complete",
                    MessageBoxButtons.OK,
                    remainingErrors == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                
                // Refresh the validation display
                RunValidation();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during auto-fix: {ex.Message}", "Auto Fix Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunAutoFixFromAnalysis()
        {
            if (_currentChain == null) return;

            var report = _validator.Validate(_currentChain);
            var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();

            if (fixableIssues.Count > 0)
            {
                var fixedCount = _autoFixService.ApplyAutoFixes(_currentChain, fixableIssues);
                if (fixedCount > 0)
                {
                    _writer.WritePropertiesFile(_currentFile, _currentChain);
                    // Reload the chain to ensure consistency
                    _currentChain = _parser.ParsePropertiesFile(_currentFile);
                }
                MessageBox.Show($"Applied {fixedCount} automatic fixes from analysis.", "Auto Fix Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RunValidation();
            }
        }

        private void RunAutoFixAllFromAnalysis()
        {
            if (_currentChain == null) return;

            var report = _validator.Validate(_currentChain);
            var allIssues = report.Issues.ToList();
            
            if (allIssues.Count == 0)
            {
                MessageBox.Show("No issues found to fix.", "Analysis Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Apply fixes for auto-fixable issues only
            var fixableIssues = allIssues.Where(i => i.IsAutoFixable).ToList();
            
            if (fixableIssues.Count == 0)
            {
                MessageBox.Show("No auto-fixable issues found.", "Auto Fix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var fixedCount = _autoFixService.ApplyAutoFixes(_currentChain, fixableIssues);
            if (fixedCount > 0)
            {
                _writer.WritePropertiesFile(_currentFile, _currentChain);
                // Reload the chain to ensure consistency
                _currentChain = _parser.ParsePropertiesFile(_currentFile);
            }
            
            var finalReport = _validator.Validate(_currentChain);
            var remainingIssues = finalReport.Issues.Count;
            
            MessageBox.Show(
                $"Auto-fix completed!\n\n" +
                $"Auto-fixable issues: {fixableIssues.Count}\n" +
                $"Successfully fixed: {fixedCount}\n" +
                $"Remaining issues: {remainingIssues}\n" +
                (remainingIssues > 0 ? "\nSome issues may require manual attention." : "\nAll issues resolved!"),
                "Auto Fix Complete",
                MessageBoxButtons.OK,
                remainingIssues == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            
            RunValidation();
        }
    }
}