import { apiClientWithAuth } from "~/services/api/client";

export interface CategoryDto {
  id: number;
  name: string;
  displayOrder?: number;
  isActive?: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface CreateCategoryRequest {
  name: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateCategoryRequest {
  name: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface DishIngredientDto {
  productId: number;
  quantity: number;
}

export interface DishDto {
  id: number;
  name: string;
  description?: string;
  categoryId?: number;
  category?: CategoryDto | null;
  price: number;
  cost: number;
  isActive: boolean;
  highlight: boolean;
  urlImage?: string;
  displayOrder?: number;
  ingredients?: DishIngredientDto[];
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface CreateDishRequest {
  name: string;
  description?: string;
  categoryId?: number;
  price: number;
  cost: number;
  isActive?: boolean;
  highlight?: boolean;
  urlImage?: string;
  displayOrder?: number;
  ingredients?: DishIngredientDto[];
}

export interface UpdateDishRequest {
  name: string;
  description?: string;
  categoryId?: number;
  price: number;
  cost: number;
  isActive?: boolean;
  highlight?: boolean;
  urlImage?: string;
  displayOrder?: number;
  ingredients?: DishIngredientDto[];
}

export const menuService = {
  // Categories
  async getCategories(restaurantId?: number): Promise<CategoryDto[]> {
    const params = new URLSearchParams();
    if (restaurantId) params.append("restaurantId", String(restaurantId));
    const query = params.toString();
    return apiClientWithAuth<CategoryDto[]>(`/api/menu/categories${query ? `?${query}` : ""}`);
  },

  async createCategory(data: CreateCategoryRequest): Promise<CategoryDto> {
    return apiClientWithAuth<CategoryDto>("/api/menu/categories", {
      method: "POST",
      body: data
    });
  },

  async updateCategory(id: number, data: UpdateCategoryRequest): Promise<CategoryDto> {
    return apiClientWithAuth<CategoryDto>(`/api/menu/categories/${id}`, {
      method: "PUT",
      body: data
    });
  },

  async deleteCategory(id: number): Promise<void> {
    return apiClientWithAuth<void>(`/api/menu/categories/${id}`, {
      method: "DELETE"
    });
  },

  // Dishes
  async getDishes(restaurantId?: number): Promise<DishDto[]> {
    const params = new URLSearchParams();
    if (restaurantId) params.append("restaurantId", String(restaurantId));
    const query = params.toString();
    return apiClientWithAuth<DishDto[]>(`/api/menu/dishes${query ? `?${query}` : ""}`);
  },

  async createDish(data: CreateDishRequest): Promise<DishDto> {
    return apiClientWithAuth<DishDto>("/api/menu/dishes", {
      method: "POST",
      body: data
    });
  },

  async updateDish(id: number, data: UpdateDishRequest): Promise<DishDto> {
    return apiClientWithAuth<DishDto>(`/api/menu/dishes/${id}`, {
      method: "PUT",
      body: data
    });
  },

  async deleteDish(id: number): Promise<void> {
    return apiClientWithAuth<void>(`/api/menu/dishes/${id}`, {
      method: "DELETE"
    });
  }
};
