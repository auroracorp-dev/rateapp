using AutoMapper;
using WebAPI.Models;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BuildingUpdateInput, Building>().ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<ReviewUpdateInput, Review>().ForMember(dest => 
        dest.buildingId, opt => opt.Ignore()) .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Review, ReviewOutput>();

        CreateMap<UserUpdateInput, User>().ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));



        /*
         * 
Code	Details
500
Undocumented
Error: response status is 500

Response body
Download
{
  "message": "Missing type map configuration or unsupported mapping.\r\n\r\nMapping types:\r\nUser -> UserOutput\r\nWebAPI.Models.User -> WebAPI.Models.UserOutput"
}
         * 
         */
    }
}