using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DR_GUI.Core.AST;
using DR_GUI.Core.Lexer;
using DR_GUI.Core.Parser;
using DR_GUI.Core.Semantic;

namespace DR_GUI.UI
{
    public partial class MainForm : Form
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

        public MainForm()
        {
            InitializeCustomComponents();
            LoadExampleCode();
        }

        private void InitializeCustomComponents()
        {
            Text = "DR Language Compiler - محول لغة DR";
            Size = new Size(1400, 900);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UIConstants.BackgroundDark;
            ForeColor = Color.White;
            Font = UIConstants.DefaultFont;

            toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = UIConstants.BackgroundToolbar,
                Padding = new Padding(10, 10, 10, 10)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            newButton = CreateStyledButton("New", UIConstants.AccentBlue);
            openButton = CreateStyledButton("Open", UIConstants.AccentGreen);
            saveButton = CreateStyledButton("Save", UIConstants.AccentYellow);
            runButton = CreateStyledButton("▶ Run Compiler", UIConstants.AccentPurple, true);

            newButton.Click += NewButton_Click;
            openButton.Click += OpenButton_Click;
            saveButton.Click += SaveButton_Click;
            runButton.Click += RunButton_Click;

            newButton.Margin = new Padding(0, 0, 10, 0);
            openButton.Margin = new Padding(0, 0, 10, 0);
            saveButton.Margin = new Padding(0, 0, 10, 0);
            runButton.Margin = new Padding(0, 0, 0, 0);

            buttonPanel.Controls.Add(newButton);
            buttonPanel.Controls.Add(openButton);
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(runButton);

            toolPanel.Controls.Add(buttonPanel);

            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400,
                BackColor = UIConstants.BackgroundDark
            };

            var codePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UIConstants.BackgroundDark,
                Padding = new Padding(10)
            };

            var codeLabel = new Label
            {
                Text = "DR Source Code",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = UIConstants.TextPrimary,
                Font = UIConstants.BoldFont,
                BackColor = Color.Transparent
            };

            codeTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = UIConstants.CodeFont,
                BackColor = UIConstants.BackgroundMedium,
                ForeColor = UIConstants.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            codePanel.Controls.Add(codeTextBox);
            codePanel.Controls.Add(codeLabel);

            outputTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = UIConstants.DefaultFont,
                Appearance = TabAppearance.FlatButtons
            };
            outputTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            outputTabs.DrawItem += Tabs_DrawItem;

            var tokensTab = new TabPage("Tokens");
            tokensTab.Name = "Tokens";
            tokensTab.BackColor = UIConstants.BackgroundDark;
            tokensGrid = CreateStyledDataGridView("Token Type", "Value", "Line");
            tokensTab.Controls.Add(tokensGrid);
            outputTabs.TabPages.Add(tokensTab);

            var symbolsTab = new TabPage("Symbol Table");
            symbolsTab.Name = "SymbolTable";
            symbolsTab.BackColor = UIConstants.BackgroundDark;
            symbolsGrid = CreateStyledDataGridView("Name", "Type", "Declared Line");
            symbolsTab.Controls.Add(symbolsGrid);
            outputTabs.TabPages.Add(symbolsTab);

            var errorsTab = new TabPage("Errors");
            errorsTab.Name = "Errors";
            errorsTab.BackColor = UIConstants.BackgroundDark;
            errorsGrid = CreateStyledDataGridView("Line", "Message");
            errorsGrid.Columns[0].Width = 80;
            errorsGrid.Columns[1].Width = 600;
            errorsTab.Controls.Add(errorsGrid);
            outputTabs.TabPages.Add(errorsTab);

            var parseTreeTab = new TabPage("Parse Tree");
            parseTreeTab.Name = "ParseTree";
            parseTreeTab.BackColor = UIConstants.BackgroundDark;

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
                ForeColor = UIConstants.TextPrimary,
                Font = UIConstants.BoldFont,
                BackColor = Color.Transparent
            };

            parseTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = UIConstants.DefaultFont,
                BackColor = UIConstants.BackgroundMedium,
                ForeColor = UIConstants.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                Indent = 20,
                ItemHeight = 22,
                FullRowSelect = false,
                HideSelection = false
            };

            parseTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            parseTreeView.DrawNode += ParseTreeView_DrawNode;

            parseTreePanel.Controls.Add(parseTreeView);
            parseTreePanel.Controls.Add(treeLabel);
            parseTreeTab.Controls.Add(parseTreePanel);
            outputTabs.TabPages.Add(parseTreeTab);

            statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 30,
                ForeColor = UIConstants.TextPrimary,
                BackColor = UIConstants.BackgroundToolbar,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = UIConstants.HeaderFont
            };

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
                Font = prominent ? UIConstants.BoldFont : UIConstants.DefaultFont,
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
                BackgroundColor = UIConstants.BackgroundDark,
                BorderStyle = BorderStyle.None,
                GridColor = UIConstants.BackgroundMedium,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = UIConstants.BackgroundMedium,
                    ForeColor = UIConstants.TextPrimary,
                    SelectionBackColor = UIConstants.AccentBlue,
                    SelectionForeColor = Color.White,
                    Font = UIConstants.DefaultFont
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = UIConstants.AccentBlue,
                    ForeColor = Color.White,
                    Font = UIConstants.HeaderFont,
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
                ? UIConstants.AccentBlue
                : UIConstants.BackgroundToolbar;

            var textColor = e.Index == tabControl.SelectedIndex
                ? Color.White
                : UIConstants.TextSecondary;

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
            statusLabel.BackColor = UIConstants.BackgroundToolbar;
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
                        statusLabel.BackColor = UIConstants.BackgroundToolbar;
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
                statusLabel.BackColor = UIConstants.AccentGreen;
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
            statusLabel.BackColor = UIConstants.AccentYellow;

            try
            {
                var scanner = new DRScanner();
                var tokens = scanner.Scan(codeTextBox.Text);

                foreach (var t in tokens)
                    tokensGrid.Rows.Add(t.Type.ToString(), t.Value, t.Line);

                var parser = new DRParserSemantic(tokens);
                parser.Parse();

                if (parser.Root != null)
                    PopulateParseTree(parser.Root);

                foreach (var s in parser.SymTab.GetAllSymbols())
                    symbolsGrid.Rows.Add(s.Name, s.DataType, s.DeclaredLine);

                if (parser.Errors.Any())
                {
                    foreach (var err in parser.Errors)
                        errorsGrid.Rows.Add(err.Line, err.Message);

                    statusLabel.Text = $"❌ Compilation failed: {parser.Errors.Count} error(s) found";
                    statusLabel.BackColor = UIConstants.AccentRed;
                    outputTabs.SelectedTab = outputTabs.TabPages["Errors"];
                }
                else
                {
                    statusLabel.Text = "✅ Compilation successful! No errors found";
                    statusLabel.BackColor = UIConstants.AccentGreen;
                    outputTabs.SelectedTab = outputTabs.TabPages["Tokens"];
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"🛑 Fatal Error: {ex.Message}";
                statusLabel.BackColor = UIConstants.AccentRed;

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
                ExpandTreeLevels(rootNode, 2);
            }

            parseTreeView.EndUpdate();
        }

        private TreeNode CreateTreeNode(ParseNode parseNode)
        {
            string displayText = $"{parseNode.Label}";
            if (parseNode.DataType != DataType.Unknown)
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

            Color backColor = UIConstants.BackgroundMedium;
            Color foreColor = UIConstants.TextPrimary;

            if (e.Node.Tag is ParseNode parseNode)
            {
                string label = parseNode.Label.ToUpper();

                if (label.Contains("PROGRAM") || label.Contains("PLANMAIN"))
                {
                    foreColor = UIConstants.AccentBlue;
                }
                else if (label.Contains("DECLARATION") || label.Contains("BLOCK"))
                {
                    foreColor = UIConstants.AccentGreen;
                }
                else if (label.Contains("IF") || label.Contains("WHILE") || label.Contains("FOR"))
                {
                    foreColor = UIConstants.AccentYellow;
                }
                else if (label.Contains("EXPRESSION") || label.Contains("CONDITION") || label.Contains("BINARY"))
                {
                    foreColor = UIConstants.AccentPurple;
                }
                else if (parseNode.DataType == DataType.File || parseNode.DataType == DataType.Duration ||
                         parseNode.DataType == DataType.Note || parseNode.DataType == DataType.Status)
                {
                    foreColor = UIConstants.AccentRed;
                }
            }

            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                backColor = UIConstants.AccentBlue;
                foreColor = Color.White;
            }

            using (Brush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.Node.Text,
                e.Node.TreeView.Font,
                e.Bounds,
                foreColor,
                TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.VerticalCenter
            );
        }
    }
}
