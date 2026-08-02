using AutoMapper;
using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Domain.Entities;

namespace SmartHealthcare.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()));
    }
}
