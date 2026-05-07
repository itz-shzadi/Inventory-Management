using AutoMapper;
using Inventory.Dtos;
using Inventory.Models;

namespace Inventory.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User,UserDto>().ReverseMap();
            CreateMap<Category, DTOs.CategoryDto>().ReverseMap();
            CreateMap<Supplier, DTOs.SupplierDto>().ReverseMap();    
        }
    }

}