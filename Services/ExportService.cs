using DJAI.Models;
using System.Text;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

namespace DJAI.Services
{
    public class ExportService(IntPtr windowHandle)
    {
        public enum ExportFormat
        {
            Text,
            Markdown,
            Html,
            Json
        }

        private readonly IntPtr _windowHandle = windowHandle;

        // Cache JsonSerializerOptions for performance
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public async Task<bool> ExportConversationAsync(Conversation conversation, ExportFormat format)
        {
            try
            {
                // Bereid bestandsinhoud voor op basis van formaat
                string fileContent = GenerateFileContent(conversation, format);
                string fileExtension = GetFileExtension(format);
                string fileName = SanitizeFileName(conversation.Title) + fileExtension;

                // Toon bestandskiezer om locatie te selecteren
                FileSavePicker savePicker = new();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, _windowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add(GetFileTypeDescription(format), [fileExtension]);
                savePicker.SuggestedFileName = fileName;

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteTextAsync(file, fileContent);
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                    return status == FileUpdateStatus.Complete;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij exporteren: {ex.Message}");
                return false;
            }
        }

        private string GenerateFileContent(Conversation conversation, ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Text => GenerateTextContent(conversation),
                ExportFormat.Markdown => GenerateMarkdownContent(conversation),
                ExportFormat.Html => GenerateHtmlContent(conversation),
                ExportFormat.Json => GenerateJsonContent(conversation),
                _ => GenerateTextContent(conversation)
            };
        }

        private static string GenerateTextContent(Conversation conversation)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine($"Gesprek: {conversation.Title}");
            _ = sb.AppendLine($"Aangemaakt op: {conversation.CreatedAt}");
            _ = sb.AppendLine($"Laatste update: {conversation.LastUpdatedAt}");
            _ = sb.AppendLine($"AI Provider: {conversation.SelectedProvider}");
            _ = sb.AppendLine(new string('-', 50));
            _ = sb.AppendLine();

            foreach (ChatMessage message in conversation.Messages)
            {
                _ = sb.AppendLine($"{message.Role}: {message.Timestamp}");
                _ = sb.AppendLine(message.Content);
                _ = sb.AppendLine();
                _ = sb.AppendLine(new string('-', 50));
                _ = sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GenerateMarkdownContent(Conversation conversation)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine($"# Gesprek: {conversation.Title}");
            _ = sb.AppendLine();
            _ = sb.AppendLine($"- **Aangemaakt op**: {conversation.CreatedAt}");
            _ = sb.AppendLine($"- **Laatste update**: {conversation.LastUpdatedAt}");
            _ = sb.AppendLine($"- **AI Provider**: {conversation.SelectedProvider}");
            _ = sb.AppendLine();
            _ = sb.AppendLine("---");
            _ = sb.AppendLine();

            foreach (ChatMessage message in conversation.Messages)
            {
                _ = sb.AppendLine($"## {message.Role} ({message.Timestamp})");
                _ = sb.AppendLine();
                _ = sb.AppendLine(message.Content);
                _ = sb.AppendLine();
                _ = sb.AppendLine("---");
                _ = sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GenerateHtmlContent(Conversation conversation)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine("<!DOCTYPE html>");
            _ = sb.AppendLine("<html lang=\"nl\">");
            _ = sb.AppendLine("<head>");
            _ = sb.AppendLine("<meta charset=\"UTF-8\">");
            _ = sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            _ = sb.AppendLine($"<title>Gesprek: {conversation.Title}</title>");
            _ = sb.AppendLine("<style>");
            _ = sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; }");
            _ = sb.AppendLine("h1 { color: #2c3e50; }");
            _ = sb.AppendLine("h2 { color: #3498db; margin-top: 30px; }");
            _ = sb.AppendLine(".meta { color: #7f8c8d; font-size: 0.9em; }");
            _ = sb.AppendLine(".message { border: 1px solid #ddd; border-radius: 8px; padding: 15px; margin: 20px 0; }");
            _ = sb.AppendLine(".user { background-color: #f8f9fa; }");
            _ = sb.AppendLine(".assistant { background-color: #e3f2fd; }");
            _ = sb.AppendLine(".system { background-color: #fff3e0; }");
            _ = sb.AppendLine(".content { white-space: pre-wrap; }");
            _ = sb.AppendLine("pre { background-color: #f5f5f5; border-radius: 4px; padding: 16px; overflow-x: auto; }");
            _ = sb.AppendLine("code { font-family: Consolas, Monaco, 'Andale Mono', 'Ubuntu Mono', monospace; }");
            _ = sb.AppendLine("</style>");
            _ = sb.AppendLine("</head>");
            _ = sb.AppendLine("<body>");
            _ = sb.AppendLine($"<h1>Gesprek: {conversation.Title}</h1>");
            _ = sb.AppendLine("<div class=\"meta\">");
            _ = sb.AppendLine($"<p><strong>Aangemaakt op:</strong> {conversation.CreatedAt}</p>");
            _ = sb.AppendLine($"<p><strong>Laatste update:</strong> {conversation.LastUpdatedAt}</p>");
            _ = sb.AppendLine($"<p><strong>AI Provider:</strong> {conversation.SelectedProvider}</p>");
            _ = sb.AppendLine("</div>");
            _ = sb.AppendLine("<hr>");

            foreach (ChatMessage message in conversation.Messages)
            {
                string roleClass = message.Role.ToString().ToLower();
                _ = sb.AppendLine($"<div class=\"message {roleClass}\">");
                _ = sb.AppendLine($"<h2>{message.Role} <span class=\"meta\">({message.Timestamp})</span></h2>");
                _ = sb.AppendLine($"<div class=\"content\">{FormatHtmlContent(message.Content)}</div>");
                _ = sb.AppendLine("</div>");
            }

            _ = sb.AppendLine("</body>");
            _ = sb.AppendLine("</html>");

            return sb.ToString();
        }

        private static string FormatHtmlContent(string content)
        {
            // Eenvoudige HTML formatting - kan vervangen worden door een volledige markdown naar HTML converter
            return content
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\n", "<br>")
                .Replace("```", "<pre><code>", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "</code></pre>", StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateJsonContent(Conversation conversation)
        {
            // Maak een anoniem object om ervoor te zorgen dat bepaalde eigenschappen worden opgenomen/uitgesloten
            var exportObject = new
            {
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt,
                conversation.LastUpdatedAt,
                conversation.SelectedProvider,
                Messages = conversation.Messages.Select(m => new
                {
                    m.Id,
                    Role = m.Role.ToString(),
                    m.Content,
                    m.Timestamp,
                    m.IsComplete
                }).ToArray()
            };

            // Use cached options
            return JsonSerializer.Serialize(exportObject, _jsonOptions);
        }

        private static string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Text => ".txt",
                ExportFormat.Markdown => ".md",
                ExportFormat.Html => ".html",
                ExportFormat.Json => ".json",
                _ => ".txt"
            };
        }

        private static string GetFileTypeDescription(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Text => "Tekstbestand",
                ExportFormat.Markdown => "Markdown bestand",
                ExportFormat.Html => "HTML bestand",
                ExportFormat.Json => "JSON bestand",
                _ => "Bestand"
            };
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string([.. fileName.Where(c => !invalidChars.Contains(c))]);
        }
    }
}