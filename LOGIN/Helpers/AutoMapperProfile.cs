﻿using AutoMapper;
using LOGIN.Dtos;
using LOGIN.Dtos.Communicates;
using LOGIN.Dtos.ReportDto;
using LOGIN.Dtos.ScheduleDtos.Blocks;
using LOGIN.Dtos.ScheduleDtos.Districts;
using LOGIN.Dtos.ScheduleDtos.Lines;
using LOGIN.Dtos.ScheduleDtos.NeighborhoodsColonies;
using LOGIN.Dtos.ScheduleDtos.RegistrationWater;
using LOGIN.Dtos.States;
using LOGIN.Dtos.UserDTOs;
using LOGIN.Entities;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CreateUserDto, UserEntity>();
        CreateMap<UserEntity, CreateUserDto>();

        CreateMap<LoginDto, UserEntity>();
        CreateMap<UserEntity, LoginDto>();

        CreateMap<LoginResponseDto, UserEntity>();
        CreateMap<UserEntity, LoginResponseDto>();
        CreateMap<LoginDto, LoginResponseDto>();

        CreateMap<CommunicateDto, CommunicateEntity>();
        CreateMap<CommunicateEntity, CommunicateDto>();
        CreateMap<CreateCommunicateDto, CommunicateEntity>();

        CreateMap<StateDto, StateEntity>();
        CreateMap<StateEntity, StateDto>();

        CreateMap<ReportDto, ReportEntity>();
        CreateMap<ReportEntity, ReportDto>();
        CreateMap<CreateReportDto, ReportEntity>();


        // Mapeo de Schedule
        CreateMap<BlockDto, BlocksEntity>();
        CreateMap<BlocksEntity, BlockDto>();
        CreateMap<BlockCreateDto, BlocksEntity>();

        CreateMap<DistrictsPointsDto, DistrictsPointsEntity>();
        CreateMap<DistrictsPointsEntity, DistrictsPointsDto>();
        CreateMap<DistrictsPointsCreateDto, DistrictsPointsEntity>();

        CreateMap<LinesDto, LinesEntity>();
        CreateMap<LinesEntity, LinesDto>();
        CreateMap<LinesCreateDto, LinesEntity>();

        CreateMap<NeighborhoodsColoniesDto, NeighborhoodsColoniesEntity>()
            .ForMember(dest => dest.Block, opt => opt.Ignore());

        CreateMap<NeighborhoodsColoniesEntity, NeighborhoodsColoniesDto>();
        CreateMap<NeighborhoodsColoniesCreateDto, NeighborhoodsColoniesEntity>();

        CreateMap<RegistrationWaterDto, RegistrationWaterEntity>()
            .ForMember(dest => dest.RegistrationWaterNeighborhoodsColonies,
                       opt => opt.MapFrom(src => src.NeighborhoodColonies
                           .Select(nc => new RegistrationWaterNeighborhoodsColoniesEntity
                           {
                               Id = Guid.NewGuid(),
                               NeighborhoodColoniesId = nc.Id
                           }).ToList()));

        CreateMap<RegistrationWaterEntity, RegistrationWaterDto>()
            .ForMember(dest => dest.NeighborhoodColonies,
                       opt => opt.MapFrom(src => src.RegistrationWaterNeighborhoodsColonies
                           .Select(rwnc => new NeighborhoodsColoniesDto
                           {
                               Id = rwnc.NeighborhoodsColonies.Id,
                               Name = rwnc.NeighborhoodsColonies.Name,
                               BlockId = rwnc.NeighborhoodsColonies.BlockId
                           }).ToList()));

        CreateMap<RegistrationWaterCreateDto, RegistrationWaterEntity>();

        CreateMap<RegistrationWaterNeighborhoodsColoniesDto, RegistrationWaterNeighborhoodsColoniesEntity>();
        CreateMap<RegistrationWaterNeighborhoodsColoniesEntity, RegistrationWaterNeighborhoodsColoniesDto>();
    }
}
