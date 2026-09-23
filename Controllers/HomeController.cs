using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public class HomeController : Controller
{
    private readonly HttpClient _httpClient;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var xmlData = new XDocument(
            new XElement("LoginModel",
                new XElement("Username", username),
                new XElement("Password", password)
            )
        );

        var content = new StringContent(xmlData.ToString(), Encoding.UTF8, "application/xml");
        var response = await _httpClient.PostAsync("http://localhost:44307/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadAsStringAsync();
            HttpContext.Session.SetString("JWT_Token", token);
            return RedirectToAction("ProtectedPage");
        }
        else
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }
    }

    public IActionResult ProtectedPage()
    {
        var token = HttpContext.Session.GetString("JWT_Token");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login");
        }
        ViewBag.Token = token;
        return View();
    }

    private async Task<string> GetProtectedDataAsync(string token)
    {
        // Set the Authorization header with the Bearer token
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:44307/api/emaillink");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();  // Return the response content
        }
        else
        {
            return $"Error: {response.StatusCode}"; // Handle error
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }


}


