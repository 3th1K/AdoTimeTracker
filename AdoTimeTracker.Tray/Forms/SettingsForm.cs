using AdoTimeTracker.Core.Models;
using AdoTimeTracker.Core.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AdoTimeTracker.Forms;

public class SettingsForm : Form
{
    private readonly ConfigService _configService;

    private NumericUpDown dailyHoursBox;
    private NumericUpDown startHourBox;
    private NumericUpDown endHourBox;
    private NumericUpDown intervalMinutesBox;

    private ListBox leaveList;

    public SettingsForm(
        ConfigService configService)
    {
        _configService = configService;

        Text = "ADO Tracker Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Width = 650;
        Height = 500;

        InitializeUi();

        LoadValues();
    }
    private void InitializeUi()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var settingsTab = new TabPage("Settings");
        var leavesTab = new TabPage("Leaves");

        #region Settings Tab

        dailyHoursBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 24,
            Width = 120
        };

        startHourBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 23,
            Width = 120
        };

        endHourBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 23,
            Width = 120
        };

        intervalMinutesBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 1440,
            Width = 120
        };

        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(20),
            AutoSize = true
        };

        settingsLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 60));

        settingsLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 40));

        settingsLayout.Controls.Add(
            new Label
            {
                Text = "Daily Hours",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

        settingsLayout.Controls.Add(
            dailyHoursBox, 1, 0);

        settingsLayout.Controls.Add(
            new Label
            {
                Text = "Reminder Start Hour",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 1);

        settingsLayout.Controls.Add(
            startHourBox, 1, 1);

        settingsLayout.Controls.Add(
            new Label
            {
                Text = "Reminder End Hour",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 2);

        settingsLayout.Controls.Add(
            endHourBox, 1, 2);

        settingsLayout.Controls.Add(
            new Label
            {
                Text = "Reminder Interval Minutes",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 3);

        settingsLayout.Controls.Add(
            intervalMinutesBox, 1, 3);

        settingsTab.Controls.Add(settingsLayout);

        #endregion

        #region Leaves Tab

        leaveList = new ListBox
        {
            Dock = DockStyle.Top,
            Height = 280
        };

        var leaveDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 140
        };

        var addButton = new Button
        {
            Text = "Add Leave",
            Width = 120,
            Height = 35
        };

        var removeButton = new Button
        {
            Text = "Remove Leave",
            Width = 120,
            Height = 35
        };

        addButton.Click += (_, _) =>
        {
            var value =
                leaveDatePicker.Value.ToString("yyyy-MM-dd");

            if (!leaveList.Items.Contains(value))
            {
                leaveList.Items.Add(value);
            }
        };

        removeButton.Click += (_, _) =>
        {
            if (leaveList.SelectedItem != null)
            {
                leaveList.Items.Remove(
                    leaveList.SelectedItem);
            }
        };

        var leavesBottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            Padding = new Padding(10),
            FlowDirection = FlowDirection.LeftToRight
        };

        leavesBottomPanel.Controls.Add(
            leaveDatePicker);

        leavesBottomPanel.Controls.Add(
            addButton);

        leavesBottomPanel.Controls.Add(
            removeButton);

        leavesTab.Controls.Add(leaveList);
        leavesTab.Controls.Add(leavesBottomPanel);

        #endregion

        tabs.TabPages.Add(settingsTab);
        tabs.TabPages.Add(leavesTab);

        Controls.Add(tabs);

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60
        };

        var saveButton = new Button
        {
            Text = "Save",
            Width = 120,
            Height = 35
        };

        saveButton.Click += Save_Click;

        saveButton.Location = new Point(
            bottomPanel.Width - 140,
            12);

        bottomPanel.Resize += (_, _) =>
        {
            saveButton.Location = new Point(
                bottomPanel.Width - saveButton.Width - 20,
                12);
        };

        bottomPanel.Controls.Add(saveButton);

        Controls.Add(bottomPanel);
    }
    private void LoadValues()
    {
        var model =
            _configService.Load();

        dailyHoursBox.Value =
            model.DailyHours;

        startHourBox.Value =
            model.StartHour;

        endHourBox.Value =
            model.EndHour;

        intervalMinutesBox.Value = model.IntervalMinutes;

        leaveList.Items.Clear();

        foreach (var leave in model.LeaveDays)
        {
            leaveList.Items.Add(
                leave.ToString("yyyy-MM-dd"));
        }
    }
    private void Save_Click(
    object? sender,
    EventArgs e)
    {
        var model =
            new SettingsViewModel
            {
                DailyHours =
                    (int)dailyHoursBox.Value,

                StartHour =
                    (int)startHourBox.Value,

                EndHour =
                    (int)endHourBox.Value,

                IntervalMinutes = 
                    (int)intervalMinutesBox.Value,

                LeaveDays =
                    leaveList.Items
                        .Cast<string>()
                        .Select(DateTime.Parse)
                        .ToList()
            };

        _configService.Save(model);

        MessageBox.Show(
            "Settings saved successfully.");

        Close();
    }
}