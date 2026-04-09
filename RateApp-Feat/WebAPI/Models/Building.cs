using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebAPI.Models;

public class Building
{
    // ============ VAR LINE ============ //

    public int buildingId { get; set; } = 0;                        // PK, FK
    public string buildingName { get; private set; } = "";                  
    public string buildingAddress { get; private set; } = "";
    public double latitude { get; private set; } = 0;
    public double longitude { get; private set; } = 0;
    public double avgRating { get; private set; } = 0;                     
    public int reviewCount { get; private set; } = 0;                      
    public int uploadUserId { get; private set; } = 0;                      // USER FK

    // ============ FK LINE ============ //

    // public bool CanUpload { get; set; } = false;                    
    [ForeignKey("uploadUserId")]                                    
    public virtual User? UploadUser { get; set; }                   // FK

    // ============ FUNC LINE ============ //

    public double Range(double lat, double lng)
    {
        return Math.Pow(this.latitude - lat, 2) + Math.Pow(this.longitude - lng, 2);
    }


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
        set => _buildingName = value?.Trim();
    }

    private string _buildingAddress;
    [Required]
    [MaxLength(50)]
    public string buildingAddress
    {
        get => _buildingAddress;
        set => _buildingAddress = value?.Trim();
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
    public int page { get; set; } = 1;
    public string? buildingName { get; set; }
    public string? buildingAddress { get; set; }
    [Range(0, Utils.pageCount)] public int minAvgRating { get; set; } = 0;
    [Range(0, Utils.pageCount)] public int maxAvgRating { get; set; } = Utils.pageCount;
    [Range(0, ushort.MaxValue)] public int minReviewCount { get; set; } = 0;
    [Range(0, ushort.MaxValue)] public int maxReviewCount { get; set; } = ushort.MaxValue;
    public string? uploadUserName { get; set; }
    // 초기 위도,경도 (0,0) = null island
    [Range(-90, 90)]   public double curLat { get; set; } = 0;     // 내 위도 => 현재 좌표 입력 자동 받게
    [Range(-180, 180)] public double curLng { get; set; } = 0;	// 내 경도 => 현재 좌표 입력 자동 받게
    // 기본 1km 범위 반경으로만 출력
    public double range { get; set; } = 1;
}

