using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<GitHubReleaseInfo> AvailableUpdates { get; private set; } = [];
    public GitHubReleaseInfo? SelectedUpdate { get; set; }
    public string? StatusMessage { get; private set; }
    public double Progress { get; private set; }

    public event Func<Task>? OnUpdateStateChangedAsync;

    public void SetState(UpdateProcessState newState, string? message = null, List<GitHubReleaseInfo>? releases = null)
    {
        CurrentState = newState;
        StatusMessage = message;
        AvailableUpdates = releases ?? [];

        if (AvailableUpdates.Any())
        {
            SelectedUpdate = AvailableUpdates.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
        }
        else
        {
            SelectedUpdate = null;
        }
        
        Progress = 0;
        NotifyStateChanged();
    }

    public void SetProgress(double progress, string? message = null)
    {
        var newProgress = Math.Clamp(progress, 0, 100);

        if (Math.Abs(Progress - newProgress) > 0.01 || StatusMessage != message)
        {
            Progress = newProgress;
            StatusMessage = message ?? StatusMessage;
            NotifyStateChanged();
        }
        else if (message != null && StatusMessage != message)
        {
            StatusMessage = message;
            NotifyStateChanged();
        }
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
        AvailableUpdates.Clear();
        SelectedUpdate = null;
        Progress = 0;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnUpdateStateChangedAsync?.Invoke();
    }
}