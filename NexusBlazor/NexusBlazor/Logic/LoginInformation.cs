using Logger;
using Microsoft.AspNetCore.Components.Authorization;
using NexusDatabaseManager;
using NexusDatabaseModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace NexusBlazor.Logic;

public class LoginInformation : INotifyPropertyChanged
{
    AuthenticationStateProvider AuthStateProvider;
    Manager manager;
    SqliteLogger logger;
    public ClaimsPrincipal? User { get; set; }

    private Project? currentProject;
    private bool _projectLoading = false;

    public Project? CurrentProject
    {
        get => currentProject;
        set
        {
            if (value != currentProject)
            {
                currentProject = value;
                OnPropertyChanged();
            }
        }
    }

    public int CurrentEmployeeId { get; set; } = 0;

    private Employee? currentEmployee;
    private bool _employeeLoading = false;

    public Employee? CurrentEmployee
    {
        get => currentEmployee;
        set
        {
            currentEmployee = value;
            OnPropertyChanged();
        }
    }

    public bool LoggedIn { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoginInformation(AuthenticationStateProvider authStateProvider, IHttpContextAccessor httpContextAccessor, Manager manager, SqliteLogger logger)
    {
        AuthStateProvider = authStateProvider;
        this.manager = manager;
        this.logger = logger;

        var sessionId = Guid.NewGuid().ToString();
        logger.Info($"New session started: {sessionId}");
        User = httpContextAccessor.HttpContext?.User;
        logger.SetSessionContext(sessionId, User?.Identity?.Name ?? "Anonymous");

        // Trigger async initialization
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await manager.EnsureInitializedAsync();
        await LoadDefaultProjectAsync();
        await LoadCurrentEmployeeAsync();
    }

    public async Task LoadDefaultProjectAsync()
    {
        if (_projectLoading || currentProject != null) return;

        _projectLoading = true;
        try
        {
            currentProject = await manager.ProjectDB.GetByIdAsync(1);
            OnPropertyChanged(nameof(CurrentProject));
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load default project: {ex.Message}");
        }
        finally
        {
            _projectLoading = false;
        }
    }

    public async Task LoadCurrentEmployeeAsync()
    {
        if (_employeeLoading || currentEmployee != null) return;

        _employeeLoading = true;
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var serialClaim = user.FindFirst(ClaimTypes.SerialNumber)?.Value;
                if (serialClaim != null && int.TryParse(serialClaim, out int id))
                {
                    CurrentEmployeeId = id;
                    currentEmployee = await manager.EmployeeDB.GetByIdAsync(id);
                    OnPropertyChanged(nameof(CurrentEmployee));
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load current employee: {ex.Message}");
        }
        finally
        {
            _employeeLoading = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}