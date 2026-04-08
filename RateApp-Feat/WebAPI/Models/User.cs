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
    public int userId { get; set; }
    public string userName { get; set; } = "";
    public string deviceSN { get; set; } = "";
    public string inviteCode { get; set; } = "";
    public DateTime createTime { get; set; } = DateTime.UtcNow;
    public Role role { get; set; } = Role.User;
    public int Point { get; set; } = 0;

    //[ForeignKey("inviteCode")]
    //public virtual Invite? Invite { get; set; }

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
}
public class UserInput
{
    private string _deviceSN;
    [Required] [MaxLength(20)] public string deviceSN
    {
        get => _deviceSN;
        set => _deviceSN = value?.Trim().ToLower();
    }
    private string _inviteCode;
    [Required] [MaxLength(20)] public string inviteCode
    {
        get => _inviteCode;
        set => _inviteCode = value?.Trim().ToLower();
    }
    private string _userName;
    [Required] [MaxLength(10)] public string userName
    {
        get => _userName;
        set => _userName = value?.Trim().ToLower();
    }
}
public class UserOutput
{
    //public string deviceSN { get; set; } = "";
    [Required] [MaxLength(10)] public string userName { get; set; } = "";
    public Role role { get; set; }= Role.User;
    public int Point { get; set; } = 0;
    public DateTime createTime { get; set; } = DateTime.UtcNow;
}
public class UserLogIn
{
    [Required][MaxLength(20)] public string deviceSN { get; set; } = "";
}

public class UserFilter
{
    public string? userName { get; set; }
    public Role? role { get; set; }
    public int? MinPoint { get; set; }
    public int? MaxPoint { get; set; }
}

public class UpdateRole
{
    public Role role { get; set; }
}