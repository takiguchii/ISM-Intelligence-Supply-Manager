namespace ISM.Domain.Modules.Assistant;

public static class AssistantSpecialists
{
    public const string General = "general";
    public const string Stock = "stock";
    public const string Pricing = "pricing";
    public const string Suppliers = "suppliers";
    public const string Finance = "finance";

    public static bool IsKnown(string? value)
        => value is General or Stock or Pricing or Suppliers or Finance;
}
