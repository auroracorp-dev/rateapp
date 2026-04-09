using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace WebAPI.Models;

public enum Role
{
    User,
    Manager,
    SuperUser,
}

public class User
{
    public User() { }
    public User(UserInput input)
    {
        this.userName = input.userName;
        this.deviceSN = input.deviceSN;
        this.inviteCode = input.inviteCode;
        this.createTime = DateTime.UtcNow;
        this.role = Role.User;
        this.Point = 0;
    }
    // ============ VAR LINE ============ //

    public int userId { get; set; }
    public string userName { get; set; } = "";
    public string deviceSN { get; set; } = "";
    public string inviteCode { get; set; } = "";
    public DateTime createTime { get; set; } = DateTime.UtcNow;
    public Role role { get; set; } = Role.User;
    public int Point { get; set; } = 0;

    //[ForeignKey("inviteCode")]
    //public virtual Invite? Invite { get; set; }

    // ============ FUNC LINE ============ //

    public bool AddPoint(int point)
    {
        if (point < 0)
            return false;

        if ((long)this.Point + point >= int.MaxValue)
        {
            this.Point = int.MaxValue;
            return false;
        }

        this.Point += point;
        return true;
    }
    public void RemovePoint(int point)
    {
        this.Point -= point;
        if (this.Point <= 0)
            this.Point = 0;
    }
    public void CheckPermissionForDelete(User user)
    {
        // 권한 체크
        bool isManager = Utils.HasManagerPermission(user.role);
        bool isOwner = user.userId == this.userId;
        if (!isManager && !isOwner)
            throw new UnauthorizedAccessException("권한 없음");
    }
}
public class UserInput
{
    private string _deviceSN;
    [Required]
    [MaxLength(20)]
    public string deviceSN
    {
        get => _deviceSN;
        set => _deviceSN = value?.Trim();
    }
    private string _inviteCode;
    [Required]
    [MaxLength(20)]
    public string inviteCode
    {
        get => _inviteCode;
        set => _inviteCode = value?.Trim();
    }
    private string _userName;
    [Required]
    [MaxLength(10)]
    public string userName
    {
        get => _userName;
        set => _userName = value?.Trim();
    }
    public void Validate()
    {
        if (TextFilter.ContainsBadWord(this.userName) || TextFilter.ContainsBadWord(this.deviceSN) || TextFilter.ContainsBadWord(this.inviteCode))
            throw new InvalidOperationException("잘못된 입력입니다.");
    }
}
public class UserOutput
{
    //public string deviceSN { get; set; } = "";
    [Required][MaxLength(10)] public string userName { get; set; } = "";
    public Role role { get; set; } = Role.User;
    public int Point { get; set; } = 0;
    public DateTime createTime { get; set; } = DateTime.UtcNow;
}
public class UserLogIn
{
    private string _deviceSN;
    [Required]
    [MaxLength(20)]
    public string deviceSN
    {
        get => _deviceSN;
        set => _deviceSN = value?.Trim();
    }
}

public class UserFilter
{
    public int page { get; set; } = 1;
    public string? userName { get; set; }
    public Role? role { get; set; }
    [Range(0, UInt16.MaxValue)] public int MinPoint { get; set; } = 0;
    [Range(0, UInt16.MaxValue)] public int MaxPoint { get; set; } = UInt16.MaxValue;
}

public class UpdateRole
{
    public Role role { get; set; }
}