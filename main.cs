using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ChatbotGemini
{
    public class apiAIService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string apiKey;
        private readonly string baseUrl;
        public apiAIService(string apiKey, string baseUrl)
        {
            this.apiKey = apiKey;
            this.baseUrl = baseUrl;
        }
        public async Task<string> GerarRespostaAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gemini/gemini-2.5-flash",
                messages = new[]
            {
                new { role = "system", content = "És um agente que somente pode responder perguntas relacionadas a perguntas de matematica. Nao utilizar markup text" },
                new { role = "user", content = prompt }
            }
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var response = await httpClient.PostAsync(baseUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            string contentText = "Erro na API";
            try
            {
                var doc = JsonDocument.Parse(responseString);
                contentText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Sem conteúdo";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao ler resposta da API: " + ex.Message);
            }
            Console.WriteLine("Resposta do Chat:");
            Console.WriteLine(contentText);
            Console.WriteLine();
            return contentText;
        }
    }
    public class Program
    {
        private static string apiKey = "sk-FgB2iX-d4U5s76MB6atiEg";
        private static string baseUrl = "https://daniel1.profmiguel.com/v1/chat/completions";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddSingleton(new apiAIService(apiKey, baseUrl));

            // Configure to listen on all interfaces
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000);
            });

            var app = builder.Build();

            app.UseCors();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // API endpoint for chat
            app.MapPost("/api/chat", async (ChatRequest request, apiAIService chatService) =>
            {
                try
                {
                    string response = await chatService.GerarRespostaAsync(request.Message);
                    return Results.Ok(new { response = response });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            // Serve the HTML page
            app.MapGet("/", async context =>
            {
                context.Response.ContentType = "text/html";
                var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "homepage.html");
                var htmlContent = await System.IO.File.ReadAllTextAsync(htmlPath);
                await context.Response.WriteAsync(htmlContent);
            });

            Console.WriteLine("ChatBot Gemini is running on http://localhost:5000");
            Console.WriteLine("Access it from your browser or from the local IP address");
            app.Run();
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}

