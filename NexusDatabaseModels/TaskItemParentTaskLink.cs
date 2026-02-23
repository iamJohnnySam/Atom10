using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace NexusDatabaseModels;

public class TaskItemParentTaskLink : INotifyPropertyChanged
{
    int parentTaskId;

    private List<TaskItem> subTasks = [];
    public List<TaskItem> SubTasks
    {
        get
        {
            return subTasks;
        }
        set
        {
            subTasks = value;
            OnPropertyChanged();
        }
    }

    public bool SubTasksComplete
    {
        get
        {
            if (SubTasks.Count == 0)
            {
                return true;
            }
            else
            {
                return !SubTasks.Any(obj => obj.IsCompleted == false);
            }
        }
    }


    public TaskItemParentTaskLink(int parentTaskId)
    {
        this.parentTaskId = parentTaskId;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
