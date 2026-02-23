using DataManagement;
using DataManagement.Enum;
using NexusDatabaseModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Project
{
    [Key]
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "Untitled Project";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? DesignCode { get; set; }
    public string? ProjectCode { get; set; }
    public string? PartNumber { get; set; }
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;
    public SalesStatus POStatus { get; set; }
    public int ProjectYear { get; set; } = DateTime.Today.Year;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTrackedProject { get; set; } = true;
    public int PrimaryDesignerId { get; set; }
    public Employee? PrimaryDesigner { get; set; }
    public string RequirementDocumentLink { get; set; } = string.Empty;

    public static TableMetadata Metadata => new(
        typeof(Project).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(ProjectId), EDataType.Key },
            { nameof(ProjectName), EDataType.Text },
            { nameof(CustomerId), EDataType.Integer },
            { nameof(DesignCode), EDataType.Text },
            { nameof(ProjectCode), EDataType.Text },
            { nameof(PartNumber), EDataType.Text },
            { nameof(Priority), EDataType.Integer },
            { nameof(POStatus), EDataType.Integer },
            { nameof(ProductId), EDataType.Integer },
            { nameof(IsActive), EDataType.Boolean },
            { nameof(IsTrackedProject), EDataType.Boolean },
            { nameof(PrimaryDesignerId), EDataType.Integer },
            { nameof(RequirementDocumentLink), EDataType.Text }
        },
        nameof(ProjectName)
    );
}
