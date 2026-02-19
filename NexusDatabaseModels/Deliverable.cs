using NexusDatabaseManager.DataManagement;
using NexusDatabaseManager.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Deliverable
{
    [Key]
    public int DeliverableId { get; set; }
    public string DeliverableName { get; set; } = "New Deliverable";
    public string DeliverableDescription { get; set; } = string.Empty;
    public string DeliverableType { get; set; } = string.Empty;

    public static TableMetadata Metadata => new(
        typeof(Deliverable).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(DeliverableId), EDataType.Key },
                { nameof(DeliverableName), EDataType.Text },
                { nameof(DeliverableDescription), EDataType.Text },
                { nameof(DeliverableType), EDataType.Text }
        },
        nameof(DeliverableName)
    );
}