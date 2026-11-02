using System.Text;

namespace MoXia.Rcl.Services
{
    public class AiNoteAssistant : IAiNoteAssistant
    {
        private const int MaxContextLength = 6000;
        private const int MaxHistoryMessages = 8;
        private readonly IAiNoteIndexService _indexService;
        private readonly IAiApiClient _apiClient;
        private readonly II18nService _i18n;

        public AiNoteAssistant(IAiNoteIndexService indexService, IAiApiClient apiClient, II18nService i18n)
        {
            _indexService = indexService;
            _apiClient = apiClient;
            _i18n = i18n;
        }

        public async Task<AiAssistantAnswer> AskAsync(
            string question,
            IReadOnlyList<AiChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            question = question.Trim();
            if (question.Length == 0)
            {
                throw new AiApiException("请输入问题。");
            }

            var hits = await _indexService.SearchAsync(question, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (hits.Count == 0)
            {
                string message = _i18n.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    ? "我没有在已建立的笔记索引中找到相关内容。请先到 AI 设置中更新向量索引，或换一种问法。"
                    : "I could not find relevant content in the note index. Update the vector index in AI settings, or try another question.";
                return new AiAssistantAnswer(message, []);
            }

            var messages = new List<AiChatMessage>
            {
                AiChatMessage.System(
                    "你是「墨匣」的 AI 笔记助手。只能依据用户提供的笔记片段回答，不得编造片段之外的事实。" +
                    "如果片段不足以回答，请明确说明没有找到，并建议用户补充笔记。" +
                    "使用与用户提问相同的语言，回答简洁、结构化，可使用 Markdown。" +
                    "引用笔记时使用来源标题，例如「来源：笔记标题」。"),
            };

            messages.AddRange(BuildHistory(history, question));
            messages.Add(AiChatMessage.User(
                $"请根据以下笔记片段回答问题。\n\n{question}\n\n笔记片段：\n{BuildContext(hits)}"));

            string answer = await _apiClient.CompleteChatAsync(messages, cancellationToken).ConfigureAwait(false);
            var sources = hits
                .GroupBy(it => it.DiaryId)
                .Select(group => group.OrderByDescending(it => it.Score).First())
                .Select(it => new AiChatSource(it.DiaryId, it.Title, it.Snippet, it.Score))
                .ToList();

            return new AiAssistantAnswer(answer, sources);
        }

        public async Task<AiNoteDraft> ConversationToNoteAsync(
            IReadOnlyList<AiChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            var turns = history
                .Where(it => (it.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(it.Content))
                .ToList();
            if (turns.Count < 2)
            {
                throw new AiApiException("当前对话内容太少，暂时无法整理成笔记。");
            }

            var transcript = new StringBuilder();
            foreach (var message in turns)
            {
                transcript.AppendLine(message.Role == "user" ? "用户：" : "AI 助手：");
                transcript.AppendLine(message.Content.Trim());
                transcript.AppendLine();
            }

            var messages = new List<AiChatMessage>
            {
                AiChatMessage.System(
                    "你负责把一段 AI 对话整理成可长期保存、可检索的 Markdown 笔记。" +
                    "保留用户真实意图、关键结论、步骤和待办，不要虚构对话中没有的信息。" +
                    "第一行输出 `# 标题`，标题不超过 30 个字符；正文使用清晰的小标题、列表和必要的代码块。" +
                    "使用与对话相同的语言。只输出标题和正文，不要解释整理过程。"),
                AiChatMessage.User(transcript.ToString()),
            };

            string raw = await _apiClient.CompleteChatAsync(messages, cancellationToken).ConfigureAwait(false);
            return ParseDraft(raw);
        }

        private static IEnumerable<AiChatMessage> BuildHistory(IReadOnlyList<AiChatMessage> history, string currentQuestion)
        {
            var trimmed = history
                .Where(it => (it.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(it.Content))
                .ToList();

            if (trimmed.Count > 0 && trimmed[^1].Role == "user" && trimmed[^1].Content.Trim() == currentQuestion)
            {
                trimmed.RemoveAt(trimmed.Count - 1);
            }

            return trimmed.TakeLast(MaxHistoryMessages);
        }

        private static string BuildContext(IReadOnlyList<AiSearchHit> hits)
        {
            var builder = new StringBuilder();
            int sourceIndex = 1;
            foreach (var hit in hits)
            {
                string section = $"--- 来源 {sourceIndex}：{hit.Title}（相似度 {hit.Score:0.00}）---\n{hit.Text}\n\n";
                if (builder.Length + section.Length > MaxContextLength)
                {
                    break;
                }

                builder.Append(section);
                sourceIndex++;
            }

            return builder.ToString().Trim();
        }

        private static AiNoteDraft ParseDraft(string raw)
        {
            string normalized = raw.Trim();
            string title = "AI 对话笔记";
            string content = normalized;

            string[] lines = normalized.Replace("\r\n", "\n").Split('\n');
            int headingIndex = Array.FindIndex(lines, line => line.TrimStart().StartsWith("# ", StringComparison.Ordinal));
            if (headingIndex >= 0)
            {
                title = lines[headingIndex].Trim()[2..].Trim();
                content = string.Join('\n', lines[(headingIndex + 1)..]).Trim();
            }
            else
            {
                int firstLineIndex = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
                if (firstLineIndex >= 0)
                {
                    title = lines[firstLineIndex].Trim();
                    if (title.Length > 40)
                    {
                        title = title[..40];
                    }
                    content = string.Join('\n', lines[(firstLineIndex + 1)..]).Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                content = normalized;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "AI 对话笔记";
            }

            return new AiNoteDraft(title, content);
        }
    }
}
