using AdoTimeTracker.Core.Models;
using System.Text.Json;

namespace AdoTimeTracker.Core.Services;

public class ConfigService
{
    private readonly string _settingsFile;
    private readonly string _leavesFile;
    private readonly LogService _logService;

    public ConfigService(LogService logService)
    {
        _settingsFile = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.json");
        _leavesFile = Path.Combine(
                    AppContext.BaseDirectory,
                    "leaves.json");
        if (string.IsNullOrWhiteSpace(_settingsFile))
        {
            throw new ArgumentException("Settings file path cannot be null or empty", nameof(_settingsFile));
        }

        if (string.IsNullOrWhiteSpace(_leavesFile))
        {
            throw new ArgumentException("Leaves file path cannot be null or empty", nameof(_leavesFile));
        }

        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        _logService.Info($"ConfigService initialized with settings file: {_settingsFile}, leaves file: {_leavesFile}");
    }

    public SettingsViewModel Load()
    {
        _logService.Info($"Loading settings from file: {_settingsFile}");

        try
        {
            if (!File.Exists(_settingsFile))
            {
                _logService.Error($"Settings file not found: {_settingsFile}");
                throw new FileNotFoundException($"Settings file not found: {_settingsFile}", _settingsFile);
            }

            var settingsJson = File.ReadAllText(_settingsFile);

            _logService.Info("Settings file read successfully, parsing JSON");

            using var doc = JsonDocument.Parse(settingsJson);

            var root = doc.RootElement;

            var settings = new SettingsViewModel
            {
                DailyHours =
                    root.GetProperty("WorkHours")
                        .GetProperty("DailyHours")
                        .GetInt32(),

                StartHour =
                    root.GetProperty("Reminder")
                        .GetProperty("StartHour")
                        .GetInt32(),

                EndHour =
                    root.GetProperty("Reminder")
                        .GetProperty("EndHour")
                        .GetInt32(),

                IntervalMinutes =
                    root.GetProperty("Reminder")
                        .GetProperty("IntervalMinutes")
                        .GetInt32(),

                LeaveDays =
                    LoadLeaves()
            };

            _logService.Info($"Settings loaded successfully - DailyHours: {settings.DailyHours}, StartHour: {settings.StartHour}, EndHour: {settings.EndHour}, IntervalMinutes: {settings.IntervalMinutes}, LeaveDays: {settings.LeaveDays.Count}");

            return settings;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while loading settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to read settings file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Access denied while loading settings: {ex.Message}");
            throw new InvalidOperationException($"Access denied to settings file: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON parsing error while loading settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse settings file. The file may be corrupted or have invalid format: {ex.Message}", ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logService.Error($"Missing required property in settings file: {ex.Message}");
            throw new InvalidOperationException($"Settings file is missing required properties: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while loading settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to load settings: {ex.Message}", ex);
        }
    }

    public void Save(SettingsViewModel model)
    {
        if (model == null)
        {
            _logService.Error("Settings model is null");
            throw new ArgumentNullException(nameof(model));
        }

        _logService.Info($"Saving settings to file: {_settingsFile}");
        _logService.Info($"Settings values - DailyHours: {model.DailyHours}, StartHour: {model.StartHour}, EndHour: {model.EndHour}, IntervalMinutes: {model.IntervalMinutes}, LeaveDays: {model.LeaveDays?.Count ?? 0}");

        try
        {
            if (!File.Exists(_settingsFile))
            {
                _logService.Error($"Settings file not found: {_settingsFile}");
                throw new FileNotFoundException($"Settings file not found: {_settingsFile}", _settingsFile);
            }

            var settingsJson =
                File.ReadAllText(_settingsFile);

            _logService.Info("Settings file read successfully, parsing JSON");

            using var doc =
                JsonDocument.Parse(settingsJson);

            var root =
                JsonSerializer.Deserialize<
                    Dictionary<string, object>>(
                    settingsJson)!;

            root["WorkHours"] = new
            {
                DailyHours = model.DailyHours
            };

            root["Reminder"] = new
            {
                StartHour = model.StartHour,
                EndHour = model.EndHour,
                IntervalMinutes = model.IntervalMinutes
            };

            _logService.Info("Writing updated settings to file");

            File.WriteAllText(
                _settingsFile,
                JsonSerializer.Serialize(
                    root,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            _logService.Info("Settings file saved successfully");

            SaveLeaves(model.LeaveDays);

            _logService.Info("Settings saved successfully");
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while saving settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to write settings file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Access denied while saving settings: {ex.Message}");
            throw new InvalidOperationException($"Access denied to settings file: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON serialization error while saving settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to serialize settings: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while saving settings: {ex.Message}");
            throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
        }
    }

    private List<DateTime> LoadLeaves()
    {
        _logService.Info($"Loading leave days from file: {_leavesFile}");

        try
        {
            if (!File.Exists(_leavesFile))
            {
                _logService.Info("Leave days file not found, returning empty list");
                return [];
            }

            var json =
                File.ReadAllText(_leavesFile);

            _logService.Info("Leave days file read successfully, parsing JSON");

            var values =
                JsonSerializer.Deserialize<List<string>>(json);

            var leaves = values?
                .Select(DateTime.Parse)
                .ToList()
                ?? [];

            _logService.Info($"Successfully loaded {leaves.Count} leave day(s)");

            return leaves;
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while loading leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to read leave days file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Access denied while loading leave days: {ex.Message}");
            throw new InvalidOperationException($"Access denied to leave days file: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON parsing error while loading leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse leave days file. The file may be corrupted or have invalid format: {ex.Message}", ex);
        }
        catch (FormatException ex)
        {
            _logService.Error($"Date format error while parsing leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse dates in leave days file. Invalid date format: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while loading leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to load leave days: {ex.Message}", ex);
        }
    }

    private void SaveLeaves(
        List<DateTime> leaves)
    {
        if (leaves == null)
        {
            _logService.Error("Leave days list is null");
            throw new ArgumentNullException(nameof(leaves));
        }

        _logService.Info($"Saving {leaves.Count} leave day(s) to file: {_leavesFile}");

        try
        {
            var data =
                leaves
                    .Select(x => x.ToString("yyyy-MM-dd"))
                    .ToList();

            _logService.Info("Serializing leave days to JSON");

            File.WriteAllText(
                _leavesFile,
                JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            _logService.Info("Leave days saved successfully");
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while saving leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to write leave days file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Access denied while saving leave days: {ex.Message}");
            throw new InvalidOperationException($"Access denied to leave days file: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON serialization error while saving leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to serialize leave days: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while saving leave days: {ex.Message}");
            throw new InvalidOperationException($"Failed to save leave days: {ex.Message}", ex);
        }
    }
}