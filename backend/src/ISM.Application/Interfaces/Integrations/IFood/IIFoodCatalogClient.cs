namespace ISM.Application.Interfaces.Integrations.IFood;

public interface IIFoodCatalogClient
{
    Task<string> UpsertCategoryAsync(int restaurantId, string name, CancellationToken ct); //atualiza ou cria categoria dentro do ifood
    Task<string> UpsertItemAsync(int restaurantId, string ifoodCategoryId, string name, string description, decimal price, CancellationToken ct); //atualiza ou cria itens dentro do ifood
    Task SetItemAvailabilityAsync(int restaurantId, string ifoodItemId, bool isAvailable, CancellationToken ct); //verifica disponibilidade de um item com base no estoque
}