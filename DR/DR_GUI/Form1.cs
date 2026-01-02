using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DR_GUI
{
    public partial class Form1 : Form
    {
        private RichTextBox codeTextBox;
        private Button runButton;
        private Button openButton;
        private Button saveButton;
        private Button newButton;
        private DataGridView tokensGrid;
        private DataGridView symbolsGrid;
        private DataGridView errorsGrid;
        private Label statusLabel;
        private TreeView parseTreeView;
        private TabControl outputTabs;
        private Panel toolPanel;
        private string currentFilePath = null;

        public Form1()
        {
            InitializeCustomComponents();
            LoadExampleCode();
        }

        private void InitializeCustomComponents()
        {
            // Main Form Settings
            Text = "DR Language Compiler - محول لغة DR";
            Size = new Size(1400, 900);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9);

            // Tool Panel
            toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(10, 10, 10, 10)
            };

            // Use FlowLayoutPanel for better button arrangement
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            // Buttons with modern style
            newButton = CreateStyledButton("New", Color.FromArgb(52, 152, 219));
            openButton = CreateStyledButton("Open", Color.FromArgb(46, 204, 113));
            saveButton = CreateStyledButton("Save", Color.FromArgb(241, 196, 15));
            runButton = CreateStyledButton("▶ Run Compiler", Color.FromArgb(155, 89, 182), true);

            newButton.Click += NewButton_Click;
            openButton.Click += OpenButton_Click;
            saveButton.Click += SaveButton_Click;
            runButton.Click += RunButton_Click;

            // Set margins for spacing between buttons
            newButton.Margin = new Padding(0, 0, 10, 0);
            openButton.Margin = new Padding(0, 0, 10, 0);
            saveButton.Margin = new Padding(0, 0, 10, 0);
            runButton.Margin = new Padding(0, 0, 0, 0);

            buttonPanel.Controls.Add(newButton);
            buttonPanel.Controls.Add(openButton);
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(runButton);

            toolPanel.Controls.Add(buttonPanel);

            // Main Split Container
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Top Split (Code Editor)
            var codePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(10)
            };

            var codeLabel = new Label
            {
                Text = "DR Source Code",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.FromArgb(236, 240, 241),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            codeTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(236, 240, 241),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            codePanel.Controls.Add(codeTextBox);
            codePanel.Controls.Add(codeLabel);

            // Bottom Tabs
            outputTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Appearance = TabAppearance.FlatButtons
            };
            outputTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            outputTabs.DrawItem += Tabs_DrawItem;

            // Tokens Tab
            var tokensTab = new TabPage("Tokens");
            tokensTab.BackColor = Color.FromArgb(30, 30, 30);
            tokensGrid = CreateStyledDataGridView("Token Type", "Value", "Line");
            tokensTab.Controls.Add(tokensGrid);
            outputTabs.TabPages.Add(tokensTab);

            // Symbols Tab
            var symbolsTab = new TabPage("Symbol Table");
            symbolsTab.BackColor = Color.FromArgb(30, 30, 30);
            symbolsGrid = CreateStyledDataGridView("Name", "Type", "Declared Line");
            symbolsTab.Controls.Add(symbolsGrid);
            outputTabs.TabPages.Add(symbolsTab);

            // Errors Tab
            var errorsTab = new TabPage("Errors");
            errorsTab.BackColor = Color.FromArgb(30, 30, 30);
            errorsGrid = CreateStyledDataGridView("Line", "Message");
            errorsGrid.Columns[0].Width = 80;
            errorsGrid.Columns[1].Width = 600;
            errorsTab.Controls.Add(errorsGrid);
            outputTabs.TabPages.Add(errorsTab);

            // Parse Tree Tab
            var parseTreeTab = new TabPage("Parse Tree");
            parseTreeTab.BackColor = Color.FromArgb(30, 30, 30);
            
            // Create a panel for TreeView with label
            var parseTreePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            var treeLabel = new Label
            {
                Text = "Parse Tree Structure",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.FromArgb(236, 240, 241),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            
            parseTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(236, 240, 241),
                BorderStyle = BorderStyle.FixedSingle,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                Indent = 20,
                ItemHeight = 22,
                FullRowSelect = false,
                HideSelection = false
            };
            
            // Custom draw for colored nodes
            parseTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            parseTreeView.DrawNode += ParseTreeView_DrawNode;
            
            parseTreePanel.Controls.Add(parseTreeView);
            parseTreePanel.Controls.Add(treeLabel);
            parseTreeTab.Controls.Add(parseTreePanel);
            outputTabs.TabPages.Add(parseTreeTab);

            // Status Label
            statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 30,
                ForeColor = Color.FromArgb(236, 240, 241),
                BackColor = Color.FromArgb(45, 45, 45),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Assemble UI
            mainSplit.Panel1.Controls.Add(codePanel);
            mainSplit.Panel2.Controls.Add(outputTabs);

            Controls.Add(mainSplit);
            Controls.Add(toolPanel);
            Controls.Add(statusLabel);
        }

        private Button CreateStyledButton(string text, Color color, bool prominent = false)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = color,
                Font = new Font("Segoe UI", prominent ? 10 : 9, prominent ? FontStyle.Bold : FontStyle.Regular),
                Height = 35,
                Width = prominent ? 150 : 100,
                Margin = new Padding(5),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, color.R + 20),
                Math.Min(255, color.G + 20),
                Math.Min(255, color.B + 20)
            );
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, color.R - 20),
                Math.Max(0, color.G - 20),
                Math.Max(0, color.B - 20)
            );
            return btn;
        }

        private DataGridView CreateStyledDataGridView(params string[] columnNames)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(60, 60, 60),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.FromArgb(236, 240, 241),
                    SelectionBackColor = Color.FromArgb(52, 152, 219),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            foreach (var name in columnNames)
                dgv.Columns.Add(name, name);

            return dgv;
        }

        private void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            var backColor = e.Index == tabControl.SelectedIndex
                ? Color.FromArgb(52, 152, 219)
                : Color.FromArgb(45, 45, 45);

            var textColor = e.Index == tabControl.SelectedIndex
                ? Color.White
                : Color.FromArgb(200, 200, 200);

            using (Brush brush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(brush, tabRect);

            TextRenderer.DrawText(
                e.Graphics,
                tabPage.Text,
                tabPage.Font,
                tabRect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            );
        }

        private void LoadExampleCode()
        {
            codeTextBox.Text = @"#ATTACH files

plan MORNING_COFFEE()
{
    file f = 5;
    duration d = 10.0;
    
    CHECK (d >= 10.0) 
    {
        SHOW ""Duration OK"";
    }
}";
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            codeTextBox.Clear();
            ClearResults();
            currentFilePath = null;
            statusLabel.Text = "New file created";
            statusLabel.BackColor = Color.FromArgb(45, 45, 45);
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = "DR Files (*.dr)|*.dr|All Files (*.*)|*.*",
                Title = "Open DR File"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        codeTextBox.Text = File.ReadAllText(dialog.FileName);
                        currentFilePath = dialog.FileName;
                        ClearResults();
                        statusLabel.Text = $"Opened: {Path.GetFileName(dialog.FileName)}";
                        statusLabel.BackColor = Color.FromArgb(45, 45, 45);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                using (var dialog = new SaveFileDialog
                {
                    Filter = "DR Files (*.dr)|*.dr|All Files (*.*)|*.*",
                    Title = "Save DR File",
                    DefaultExt = "dr"
                })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                        currentFilePath = dialog.FileName;
                    else
                        return;
                }
            }

            try
            {
                File.WriteAllText(currentFilePath, codeTextBox.Text);
                statusLabel.Text = $"Saved: {Path.GetFileName(currentFilePath)}";
                statusLabel.BackColor = Color.FromArgb(46, 204, 113);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            ClearResults();
            statusLabel.Text = "Compiling...";
            statusLabel.BackColor = Color.FromArgb(241, 196, 15);

            try
            {
                // 1. Run Scanner
                var scanner = new DRScanner();
                var tokens = scanner.Scan(codeTextBox.Text);

                foreach (var t in tokens)
                    tokensGrid.Rows.Add(t.Type.ToString(), t.Value, t.Line);

                // 2. Run Parser + Semantic Analysis
                var parser = new DRParserSemantic(tokens);
                parser.Parse();

                // 3. Display Parse Tree
                if (parser.Root != null)
                    PopulateParseTree(parser.Root);

                // 4. Display Symbol Table
                foreach (var s in parser.SymTab.GetAllSymbols())
                    symbolsGrid.Rows.Add(s.Name, s.DataType, s.DeclaredLine);

                // 5. Display Errors
                if (parser.Errors.Any())
                {
                    foreach (var err in parser.Errors)
                        errorsGrid.Rows.Add(err.Line, err.Message);

                    statusLabel.Text = $"❌ Compilation failed: {parser.Errors.Count} error(s) found";
                    statusLabel.BackColor = Color.FromArgb(231, 76, 60);
                    outputTabs.SelectedTab = outputTabs.TabPages["Errors"];
                }
                else
                {
                    statusLabel.Text = "✅ Compilation successful! No errors found";
                    statusLabel.BackColor = Color.FromArgb(46, 204, 113);
                    outputTabs.SelectedTab = outputTabs.TabPages["Tokens"];
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"🛑 Fatal Error: {ex.Message}";
                statusLabel.BackColor = Color.FromArgb(231, 76, 60);
                
                errorsGrid.Rows.Add(0, $"FATAL: {ex.Message}");
                outputTabs.SelectedTab = outputTabs.TabPages["Errors"];
            }
        }

        private void ClearResults()
        {
            tokensGrid.Rows.Clear();
            symbolsGrid.Rows.Clear();
            errorsGrid.Rows.Clear();
            parseTreeView.Nodes.Clear();
        }

        private void PopulateParseTree(ParseNode root)
        {
            parseTreeView.BeginUpdate();
            parseTreeView.Nodes.Clear();
            
            if (root != null)
            {
                var rootNode = CreateTreeNode(root);
                parseTreeView.Nodes.Add(rootNode);
                rootNode.Expand();
                // Auto-expand first few levels
                ExpandTreeLevels(rootNode, 2);
            }
            
            parseTreeView.EndUpdate();
        }

        private TreeNode CreateTreeNode(ParseNode parseNode)
        {
            string displayText = $"{parseNode.Label}";
            if (!string.IsNullOrEmpty(parseNode.DataType) && parseNode.DataType != "unknown")
                displayText += $" : {parseNode.DataType}";
            if (parseNode.Line > 0)
                displayText += $" (line {parseNode.Line})";

            var treeNode = new TreeNode(displayText)
            {
                Tag = parseNode
            };

            foreach (var child in parseNode.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        private void ExpandTreeLevels(TreeNode node, int levels)
        {
            if (levels > 0)
            {
                node.Expand();
                foreach (TreeNode child in node.Nodes)
                {
                    ExpandTreeLevels(child, levels - 1);
                }
            }
        }

        private void ParseTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            Color backColor = Color.FromArgb(40, 40, 40);
            Color foreColor = Color.FromArgb(236, 240, 241);
            
            // Color code different node types
            if (e.Node.Tag is ParseNode parseNode)
            {
                string label = parseNode.Label.ToUpper();
                
                if (label.Contains("PROGRAM") || label.Contains("PLANMAIN"))
                {
                    foreColor = Color.FromArgb(52, 152, 219); // Blue
                }
                else if (label.Contains("DECLARATION") || label.Contains("BLOCK"))
                {
                    foreColor = Color.FromArgb(46, 204, 113); // Green
                }
                else if (label.Contains("IF") || label.Contains("WHILE") || label.Contains("FOR"))
                {
                    foreColor = Color.FromArgb(241, 196, 15); // Yellow
                }
                else if (label.Contains("EXPRESSION") || label.Contains("CONDITION") || label.Contains("BINARY"))
                {
                    foreColor = Color.FromArgb(155, 89, 182); // Purple
                }
                else if (parseNode.DataType == "int" || parseNode.DataType == "double" || 
                         parseNode.DataType == "string" || parseNode.DataType == "bool")
                {
                    foreColor = Color.FromArgb(231, 76, 60); // Red
                }
            }

            // Highlight selected node
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                backColor = Color.FromArgb(52, 152, 219);
                foreColor = Color.White;
            }

            // Draw background
            using (Brush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            // Draw text
            TextRenderer.DrawText(
                e.Graphics,
                e.Node.Text,
                e.Node.TreeView.Font,
                e.Bounds,
                foreColor,
                TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.VerticalCenter
            );
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
