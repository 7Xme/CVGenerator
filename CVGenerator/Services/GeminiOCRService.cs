using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CVGenerator.Models;
using Serilog;

namespace CVGenerator.Services;

public class GeminiOCRService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;

    public GeminiOCRService(string apiKey, string modelName = "gemini-1.5-pro")
    {
        _apiKey = apiKey;
        _modelName = modelName;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<GeminiCVResponse> ExtractCVFromImageAsync(byte[] imageBytes, string mimeType = "image/jpeg")
    {
        try
        {
            Log.Information("Starting OCR extraction from image ({Size} bytes)", imageBytes.Length);

            string base64Image = Convert.ToBase64String(imageBytes);
            string systemPrompt = BuildSystemPrompt();

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = systemPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    topP = 0.95,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            Log.Information("Sending request to Gemini API...");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            Log.Information("Received response from Gemini API");

            return ParseGeminiResponse(responseJson);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error communicating with Gemini API");
            throw new InvalidOperationException($"فشل الاتصال بـ Gemini API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Log.Error(ex, "Request to Gemini API timed out");
            throw new InvalidOperationException("انتهت مهلة الطلب. تحقق من اتصال الإنترنت وحاول مرة أخرى.", ex);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Failed to parse Gemini API response");
            throw new InvalidOperationException($"خطأ في تحليل رد Gemini: {ex.Message}", ex);
        }
    }

    private static string BuildSystemPrompt()
    {
        return @"
أنت خبير في استخراج البيانات من نماذج CV المُملأة يدوياً. 
قم بتحليل الصورة المرفقة واستخرج جميع المعلومات بدقة.

التعليمات:
1. قم بـ OCR دقيق للنصوص العربية والفرنسية
2. صحح الأخطاء الإملائية والنحوية تلقائياً
3. حوّل التواريخ إلى صيغة موحدة (YYYY-MM)
4. حسّن الصياغات لتكون احترافية
5. أكمل الحقول الناقصة بـ null إن لم تتوفر
6. أرجع النتيجة كـ JSON صالح فقط (Valid JSON) بدون أي نص إضافي

هيكل JSON المطلوب:
{
  ""cv_data"": {
    ""personal_info"": {
      ""full_name"": ""..."",
      ""full_name_latin"": ""..."",
      ""phone_primary"": ""..."",
      ""phone_secondary"": ""..."",
      ""email"": ""..."",
      ""national_id"": ""..."",
      ""address"": ""..."",
      ""driving_license"": ""..."",
      ""date_of_birth"": ""..."",
      ""photo_path"": """"
    },
    ""education"": [
      {
        ""degree"": ""..."",
        ""institution"": ""..."",
        ""year"": ""..."",
        ""mention"": ""..."",
        ""description"": ""...""
      }
    ],
    ""experience"": [
      {
        ""company"": ""..."",
        ""position"": ""..."",
        ""start_date"": ""..."",
        ""end_date"": ""..."",
        ""tasks"": [""...""]
      }
    ],
    ""skills"": [
      { ""name"": ""..."", ""level"": ""..."" }
    ],
    ""languages"": [
      { ""name"": ""..."", ""level"": ""..."" }
    ],
    ""interests"": [""...""],
    ""summary"": ""...""
  },
  ""metadata"": {
    ""confidence_score"": 0.95,
    ""fields_detected"": [""...""],
    ""suggestions"": [""...""],
    ""warnings"": [""...""]
  }
}

ملاحظات خاصة:
- إذا كان النموذج بالعربية: استخرج البيانات بالعربية
- إذا كان بالفرنسية: استخرج بالفرنسية
- إذا كان مختلطاً: حافظ على اللغة الأصلية لكل حقل
- للمستويات الدراسية: حدد ""Baccalauréat"" أو ""Licence"" أو ""Master"" إن أمكن
- للغات: حدد المستوى (Courant, Moyen, Débutant, Bien...)
إذا كانت الصورة غير واضحة أو فارغة:
- confidence_score = 0.0
- warnings = [""الصورة غير واضحة""]
- cv_data = null
";
    }

    private GeminiCVResponse ParseGeminiResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            Log.Warning("No candidates found in Gemini response");
            return new GeminiCVResponse
            {
                CVData = null,
                Metadata = new Metadata
                {
                    ConfidenceScore = 0.0,
                    Warnings = new List<string> { "لم يتم العثور على مرشحين في رد Gemini" }
                }
            };
        }

        var text = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            return new GeminiCVResponse
            {
                CVData = null,
                Metadata = new Metadata
                {
                    ConfidenceScore = 0.0,
                    Warnings = new List<string> { "الرد فارغ" }
                }
            };
        }

        text = text.Replace("```json", "").Replace("```", "").Trim();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var geminiResponse = JsonSerializer.Deserialize<GeminiCVResponse>(text, options);
        return geminiResponse ?? new GeminiCVResponse
        {
            CVData = null,
            Metadata = new Metadata
            {
                ConfidenceScore = 0.0,
                Warnings = new List<string> { "فشل تحليل JSON" }
            }
        };
    }
}
