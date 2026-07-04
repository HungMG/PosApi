using PosWebAdmin.Models;

namespace PosWebAdmin.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Trỏ thẳng lên con Server thật trên mạng
            _httpClient.BaseAddress = new Uri("https://pos-cafe-api-52yt.onrender.com");
        }

        // Lấy danh sách danh mục
        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _httpClient.GetFromJsonAsync<List<Category>>("/api/categories");
                return categories ?? new List<Category>();
            }
            catch
            {
                return new List<Category>();
            }
        }
        // Hàm gửi lệnh tạo Danh mục mới lên Server
        public async Task<bool> CreateCategoryAsync(Category category)
        {
            try
            {
                // Gửi trực tiếp nguyên đối tượng category có chứa ParentId sang Backend
                var response = await _httpClient.PostAsJsonAsync("/api/categories", category);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // CÁC HÀM QUẢN LÝ DANH MỤC (SỬA / XÓA)
        // ==========================================
        public async Task<bool> UpdateCategoryAsync(int id, Category category)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/categories/{id}", category);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/categories/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
        // ==========================================
        // CÁC HÀM QUẢN LÝ MÓN ĂN (PRODUCTS)
        // ==========================================
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var products = await _httpClient.GetFromJsonAsync<List<Product>>("/api/products");
                return products ?? new List<Product>();
            }
            catch { return new List<Product>(); }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/products", product);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/products/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}