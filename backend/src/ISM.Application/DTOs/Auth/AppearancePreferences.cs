using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class AppearancePreferences
{
    [Required, RegularExpression("^(dark|light)$")]
    public string Theme { get; set; } = "dark";
    [Required, RegularExpression("^(comfortable|compact)$")]
    public string Density { get; set; } = "comfortable";
    [Range(90, 115)]
    public int FontScale { get; set; } = 100;
    public bool ReduceMotion { get; set; }
    public bool HighContrast { get; set; }
}
