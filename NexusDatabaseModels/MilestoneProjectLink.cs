using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace NexusDatabaseModels;

public class MilestoneProjectLink : INotifyPropertyChanged
{
    int projectId;

    private List<Milestone> incompleteItems = [];
    public List<Milestone> IncompleteItems
    {
        get
        {
            return incompleteItems;
        }
        set
        {
            incompleteItems = value;
            OnPropertyChanged();
        }
    }

    private List<Milestone> completedItems = [];
    public List<Milestone> CompletedItems
    {
        get
        {
            return completedItems;
        }
        set
        {
            completedItems = value;
            OnPropertyChanged();
        }
    }

    public MilestoneProjectLink(int projectId)
    {
        this.projectId = projectId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
