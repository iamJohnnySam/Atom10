using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class TaskItem
{
    [Key]
    public int TaskId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public DateTime StartedOn { get; set; } = DateTime.Now;
    public DateTime? CompletedOn { get; set; }
    public DateTime Deadline { get; set; } = DateTime.Now;
    public int ResponsibleId { get; set; }
    public Employee? Responsible { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsBlocking { get; set; } = true; // Is blocking the timeline and not a background task
    public int? ParentTaskId { get; set; }
    public bool PriorityTask { get; set; } = false;


    public static TableMetadata Metadata => new(
        typeof(TaskItem).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(TaskId), EDataType.Key },
            { nameof(ProjectId), EDataType.Integer },
            { nameof(Title), EDataType.Text },
            { nameof(Description), EDataType.Text },
            { nameof(CreatedOn), EDataType.Date },
            { nameof(StartedOn), EDataType.Date },
            { nameof(CompletedOn), EDataType.Date },
            { nameof(Deadline), EDataType.Date },
            { nameof(ResponsibleId), EDataType.Integer },
            { nameof(IsCompleted), EDataType.Boolean },
            { nameof(IsBlocking), EDataType.Boolean },
            { nameof(ParentTaskId), EDataType.Integer },
            { nameof(PriorityTask), EDataType.Boolean }
        },
        nameof(Deadline)
    );
}