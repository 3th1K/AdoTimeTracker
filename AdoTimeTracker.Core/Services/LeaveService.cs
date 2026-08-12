using System.Text.Json;

namespace AdoTimeTracker.Core.Services;

public class LeaveService
{
    private readonly string _filePath;
    private readonly LogService _logService;

    public LeaveService(LogService logService)
    {
        _filePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "leaves.json");
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Failed to get directory name for leave days file path."));
        }

        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        _logService.Info($"LeaveService initialized with file path: {_filePath}");
    }

    public List<DateTime> GetLeaves()
    {
        _logService.Info($"Loading leave days from file: {_filePath}");

        try
        {
            if (!File.Exists(_filePath))
            {
                _logService.Info("Leave days file not found, returning empty list");
                return [];
            }

            var json = File.ReadAllText(_filePath);

            _logService.Info("Leave days file read successfully, parsing JSON");

            var dates = JsonSerializer.Deserialize<List<string>>(json);

            if (dates is null)
            {
                _logService.Info("No leave days found in file, returning empty list");
                return [];
            }

            var leaves = dates
                .Select(DateTime.Parse)
                .Select(d => d.Date)
                .ToList();

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
}