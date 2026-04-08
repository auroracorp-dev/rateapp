using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebAPI.Models;

public class Building
{
    public int buildingId { get; set; } = 0;                        // PK, FK
    public string buildingName { get; private set; } = "";                  
    public string buildingAddress { get; private set; } = "";
    public double latitude { get; private set; } = 0;
    public double longitude { get; private set; } = 0;
    public double avgRating { get; private set; } = 0;                     
    public int reviewCount { get; private set; } = 0;                      
    public int uploadUserId { get; private set; } = 0;                      // USER FK
    // public bool CanUpload { get; set; } = false;                    
    [ForeignKey("uploadUserId")]                                    
    public virtual User? UploadUser { get; set; }                   // FK

    public Building Create(BuildingInput input, int userId)
    {
        return new Building
        {
            buildingName = input.buildingName,
            buildingAddress = input.buildingAddress,
            latitude = input.latitude,
            longitude = input.longitude,
            uploadUserId = userId,
            avgRating = 0,
            reviewCount = 0
        };
    }
    // 건물이 스스로의 상태를 업데이트하는 '책임'을 가짐
    public void AddNewReview(double newRating)
    {
        double totalRating = (this.avgRating * this.reviewCount) + newRating;
        this.reviewCount++;
        this.avgRating = Math.Round((totalRating / this.reviewCount) * 2) / 2.0;
    }
    public void UpdateReview(double originRating, double newRating)
    {
        if (this.reviewCount == 0)
            return;

        double totalRating = (this.avgRating * this.reviewCount)- originRating+ newRating;
        this.avgRating = Math.Round((totalRating / this.reviewCount) * 2) / 2.0;
    }
    public void RemoveReview(double reviewRating)
    {
        if (this.reviewCount <= 1)
        {
            this.reviewCount = 0;
            this.avgRating = 0;
            return;
        }

        double totalRating = (this.avgRating * this.reviewCount) - reviewRating;

        this.reviewCount--;

        if (this.reviewCount <= 0)
        {
            this.reviewCount = 0;
            this.avgRating = 0;
            return;
        }

        // x.5 단위로 반올림
        this.avgRating = Math.Round((totalRating / this.reviewCount) * 2) / 2.0;

        // 0~10 사이 제한
        this.avgRating = Math.Clamp(this.avgRating, 0, 10);
    }
    public void CheckPermissionForDelete(User user)
    {
        // 권한 체크
        bool isManager = Utils.HasManagerPermission(user.role);
        bool isOwner = user.userId == this.uploadUserId;
        if (!isManager && !isOwner)
            throw new UnauthorizedAccessException("권한 없음");
    }
}
public class BuildingInput
{
    private string _buildingName;
    [Required]
    [MaxLength(50)]
    public string buildingName
    {
        get => _buildingName;
        set => _buildingName = value?.Trim().ToLower();
    }

    private string _buildingAddress;
    [Required]
    [MaxLength(50)]
    public string buildingAddress
    {
        get => _buildingAddress;
        set => _buildingAddress = value?.Trim().ToLower();
    }

    [Required] public double latitude { get; set; }
    [Required] public double longitude { get; set; }

    public void Validate()
    {
        if (TextFilter.ContainsBadWord(buildingName) || TextFilter.ContainsBadWord(buildingAddress))
            throw new InvalidOperationException("잘못된 입력입니다.");

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new InvalidOperationException("경도/위도 범위 오류");
    }
}

public class BuildingOutput
{
    public string buildingName { get; set; } = "";
    public string buildingAddress { get; set; } = "";
    public double avgRating { get; set; } = 0;
    public int reviewCount { get; set; } = 0;
    public int uploadUserId { get; set; } = 0;
}

public class BuildingFilter
{
    public string? buildingName { get; set; }
    public string? buildingAddress { get; set; }
    public double? minAvgRating { get; set; }
    public double? maxAvgRating { get; set; }
    public int? minReviewCount { get; set; }
    public int? maxReviewCount { get; set; }
    public string? uploadUserName { get; set; }
}

