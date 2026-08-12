using AdoTimeTracker.Core.Models;
using System.Diagnostics;

namespace AdoTimeTracker.Tray.Forms;

internal partial class StatusForm : Form
{
    private readonly TreeView _treeView;

    public StatusForm(
        TimeTrackingSummary summary)
    {
        Text = "Azure DevOps Tracker Status";

        Width = 1000;
        Height = 700;

        StartPosition =
            FormStartPosition.CenterScreen;

        var splitContainer =
            new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 50
            };

        Controls.Add(splitContainer);

        splitContainer.Panel1.Controls.Add(
            CreateSummaryPanel(summary));

        _treeView =
            CreateTree(summary);

        splitContainer.Panel2.Controls.Add(
            _treeView);
    }

    private Control CreateSummaryPanel(
    TimeTrackingSummary summary)
    {
        var groupBox = new GroupBox
        {
            Text = "Sprint Summary",
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true
        };

        table.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 300));

        table.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        AddRow(
            "Sprint",
            summary.SprintName);

        AddRow(
            "Working Days Elapsed",
            summary.WorkingDaysElapsed.ToString());

        AddRow(
            "Leave Days Applied",
            summary.LeaveDaysApplied.ToString());

        AddRow(
            "Expected Hours",
            summary.ExpectedHours.ToString("0.##"));

        AddRow(
            "Logged Hours",
            summary.LoggedHours.ToString("0.##"));

        AddRow(
            "Pending Hours",
            summary.PendingHours.ToString("0.##"));
        groupBox.Controls.Add(table);

        return groupBox;

        void AddRow(
            string label,
            string value)
        {
            var row = table.RowCount++;

            table.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            table.Controls.Add(
                new Label
                {
                    Text = label,
                    AutoSize = true,
                    Font = new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold),
                    Margin = new Padding(5)
                },
                0,
                row);

            table.Controls.Add(
                new Label
                {
                    Text = value,
                    AutoSize = true,
                    Margin = new Padding(5)
                },
                1,
                row);
        }
    }

    private TreeView CreateTree(
        TimeTrackingSummary summary)
    {
        var tree =
            new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };

        foreach (var stateGroup in summary.WorkItemsSummaries)
        {
            var stateNode =
                new TreeNode(
                    $"{stateGroup.State} ({stateGroup.Count})");

            foreach (var workItem in stateGroup.WorkItems)
            {
                var workItemNode =
                    new TreeNode(
                        $"#{workItem.Id} - {workItem.Title}")
                    {
                        Tag = workItem
                    };

                workItemNode.Nodes.Add(
                    $"Completed Work : {workItem.CompletedWork}");

                workItemNode.Nodes.Add(
                    $"Remaining Work : {workItem.RemainingWork}");

                workItemNode.Nodes.Add(
                    $"State : {workItem.State}");

                workItemNode.Nodes.Add(
                    "🌐 Open Work Item")
                    .Tag = workItem.Link;

                stateNode.Nodes.Add(
                    workItemNode);
            }

            tree.Nodes.Add(
                stateNode);
        }

        tree.NodeMouseDoubleClick +=
            Tree_NodeMouseDoubleClick;

        tree.ContextMenuStrip =
            BuildContextMenu();

        return tree;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu =
            new ContextMenuStrip();

        menu.Items.Add(
            "Open Work Item",
            null,
            (_, _) => OpenSelectedWorkItem());

        return menu;
    }

    private void Tree_NodeMouseDoubleClick(
        object? sender,
        TreeNodeMouseClickEventArgs e)
    {
        if (e.Node.Tag is string url)
        {
            OpenUrl(url);
            return;
        }

        if (e.Node.Tag is WorkItemInfo workItem)
        {
            OpenUrl(workItem.Link);
        }
    }

    private void OpenSelectedWorkItem()
    {
        var node =
            _treeView.SelectedNode;

        if (node == null)
            return;

        if (node.Tag is WorkItemInfo workItem)
        {
            OpenUrl(workItem.Link);
        }
        else if (node.Tag is string url)
        {
            OpenUrl(url);
        }
    }

    private static void OpenUrl(
        string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Process.Start(
            new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
    }
}