using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record MyProfile(string Name, string? AvatarDataUrl);

public sealed record UpdateMyProfileRequest(
    [Required, MaxLength(150)] string Name,
    [MaxLength(273_092)] string? AvatarDataUrl);
