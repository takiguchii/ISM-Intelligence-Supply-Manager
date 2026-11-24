namespace ISM.Application.DTOs.Menu;

public sealed class IFoodCatalogSyncResultDto
{
    public int CategoriesSynced { get; set; }
    public int ItemsSynced { get; set; }
}

public sealed class IFoodCatalogStatusDto
{
    public int TotalCategories {get; set;}
    public int CategoriesSynced {get; set;}
    public int TotalDishes {get; set;}
    public int DishesSynced {get; set;}
}