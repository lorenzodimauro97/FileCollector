
using System;
using System.Threading.Tasks;
using FileCollector.Models;

namespace FileCollector.Services;

public enum UpdateProcessState
{
    Idle,
    Checking,
    UpdateAvailable,
    Downloading,
    Extracting,
    ReadyToApply,
    Applying,
    Error
}

public class UpdateStateService
{
    public UpdateProcessState CurrentState { get; private set; } = UpdateProcessState.Idle;
    public GitHubReleaseInfo? AvailableUpdateInfo { get; private set; }
    public string? StatusMessage { get; private set; }
    public double Progress { get; private set; }

    public event Func<Task>? OnUpdateStateChangedAsync;

    public void SetState(UpdateProcessState newState, string? message = null, GitHubReleaseInfo? releaseInfo = null)
    {
        CurrentState = newState;
        StatusMessage = message;
        AvailableUpdateInfo = releaseInfo ?? AvailableUpdateInfo;
        Progress = 0;
        NotifyStateChanged();
    }

    public void SetProgress(double progress, string? message = null)
    {
        Progress = Math.Clamp(progress, 0, 100);
        StatusMessage = message ?? StatusMessage;
        NotifyStateChanged();
    }
    
    public void SetError(string errorMessage)
    {
        CurrentState = UpdateProcessState.Error;
        StatusMessage = errorMessage;
        Progress = 0;
        NotifyStateChanged();
    }

    public void ResetState()
    {
        CurrentState = UpdateProcessState.Idle;
        StatusMessage = null;
        AvailableUpdateInfo = null;
        Progress = 0;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnUpdateStateChangedAsync?.Invoke();
    }
}