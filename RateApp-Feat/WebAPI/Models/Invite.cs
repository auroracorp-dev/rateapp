using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebAPI.Models;
public class Invite
{

    // ============ VAR LINE ============ //

    public int inviteId { get; set; }
    public string inviteCode { get; private set; }
    public DateTime createDate { get; private set; } = DateTime.UtcNow;
    public int? createdBy { get; private set; } = null;
    public int? usedBy { get; private set; } = null;
    public DateTime? usedDate { get; private set; }
    public bool IsUsed { get; private set; } = false;

    // ============ FK LINE ============ //

    [ForeignKey("createdBy")]
    public virtual User? Creator { get; set; }
    [ForeignKey("usedBy")]
    public virtual User? UsedByUser { get; set; }

    // ============ FUNC LINE ============ //
    public void AllocTo(User user)
    {
        this.usedBy = user.userId;
    }
    public void SetInvite(int userId, string code)
    {
        this.inviteCode = code;
        this.createDate = DateTime.UtcNow;
        this.createdBy = userId;
        this.IsUsed = false;
    }
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
    public void CheckPermissionForDelete(User user)
    {
        // 권한 체크
        //bool isManager = Utils.HasManagerPermission(user.role);
        //bool isOwner = user.userId == this.createdBy;
        //if (!isManager && !isOwner)
        //    throw new UnauthorizedAccessException("권한 없음");
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

    public DateTime createStartDate { get; set; } = DateTimeHelper.Utc(1900, 1, 1);
    public DateTime createLastDate { get; set; } = DateTimeHelper.Utc(2099, 12, 31);
    public string? createdByName { get; set; }
}

