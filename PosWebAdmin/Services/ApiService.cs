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
    }
}