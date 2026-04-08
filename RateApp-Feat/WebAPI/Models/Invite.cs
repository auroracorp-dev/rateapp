using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebAPI.Models;
public class Invite
{
    public int inviteId { get; set; }
    public string inviteCode { get; set; }
    public DateTime createDate { get; set; } = DateTime.UtcNow;
    public int? createdBy { get; set; } = null;
    public int? usedBy { get; set; } = null;
    public DateTime? usedDate { get; set; }
    public bool IsUsed { get; set; } = false;

    [ForeignKey("createdBy")]
    public virtual User? Creator { get; set; }

    [ForeignKey("usedBy")]
    public virtual User? UsedByUser { get; set; }

    public void SetUsed(int userId)
    {
        this.IsUsed = true;
        this.usedBy = userId;
        this.usedDate = DateTime.UtcNow;
    }
    public void SetUsedNull()
    {
        this.IsUsed = false;
        this.usedBy = null;
        this.usedDate = null;
    }
}
//public class InviteInput
//{

//}
public class InviteOutput
{
    public int inviteId { get; set; } = 0;
    public string inviteCode { get; set; } = "";
    public bool IsUsed { get; set; } = false;
    public string? createdByName { get; set; }

}
public class InviteFilter
{
    public int page { get; set; } = 1;
    public bool? IsUsed { get; set; }
    public DateTime? createStartDate { get; set; } 
    public DateTime? createLastDate { get; set; }
    public string? createdByName { get; set; }
}

