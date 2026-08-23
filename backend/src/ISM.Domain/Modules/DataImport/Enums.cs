namespace ISM.Domain.Modules.DataImport;

public enum DataSourceType
{
    ManualEntry = 0,
    Csv = 1,
    Excel = 2,
    XmlNfe = 3,
    RestWebhook = 4,
    RestApiPull = 5,
    MqttIoT = 6
}

public enum UpsertStrategy
{
    InsertOnly = 0,
    MergeById = 1,
    MergeByNameAndRestaurant = 2
}

public enum TargetImportEntity
{
    Product = 1,
    Supplier = 2,
    Category = 3,
    Dish = 4,
    DishIngredient = 5,
    SalesOrder = 6
}