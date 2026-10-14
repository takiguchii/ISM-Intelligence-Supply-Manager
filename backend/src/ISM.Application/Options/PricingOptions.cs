namespace ISM.Application.Options;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";
    public decimal DefaultTargetMarginPercent { get; set; } = 65m; //define o percentual de margem padrão
}