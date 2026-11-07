using System.ComponentModel;

namespace TurniaCore.Data.Entities.BaseEntities;

public class BaseEntity
{
    [DisplayName("Created")]
    public DateTime Created { get; set; }
    [DisplayName("Created By")]
    public string CreatedBy { get; set; } = null!;
    public Guid CreatedById { get; set; }
    [DisplayName("Modified")]
    public DateTime? Modified { get; set; }
    [DisplayName("Modified By")]
    public string? ModifiedBy { get; set; }
    public Guid? ModifiedById { get; set; }
}
