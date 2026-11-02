using System.Text;
using System.Text.RegularExpressions;

namespace MoXia.Rcl.Services
{
    public static partial class AiMarkdownChunker
    {
        private const int MaxChunkLength = 900;
        private const int OverlapLength = 120;

        [GeneratedRegex(@"\r?\n\s*\r?\n")]
        private static partial Regex ParagraphRegex();

        [GeneratedRegex(@"(?<=[。！？!?；;.!?])\s*")]
        private static partial Regex SentenceRegex();

        public static IReadOnlyList<string> Split(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            var blocks = ParagraphRegex().Split(normalized)
                .SelectMany(SplitBlock)
                .Where(it => !string.IsNullOrWhiteSpace(it))
                .ToList();

            if (blocks.Count == 0)
            {
                return [];
            }

            var chunks = new List<string>();
            var builder = new StringBuilder();

            foreach (string block in blocks)
            {
                if (builder.Length > 0 && builder.Length + block.Length + 2 > MaxChunkLength)
                {
                    string chunk = builder.ToString().Trim();
                    if (chunk.Length > 0)
                    {
                        chunks.Add(chunk);
                    }

                    string overlap = GetOverlap(chunk);
                    builder.Clear();
                    if (overlap.Length > 0)
                    {
                        builder.AppendLine(overlap);
                    }
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(block.Trim());
            }

            string tail = builder.ToString().Trim();
            if (tail.Length > 0)
            {
                chunks.Add(tail);
            }

            return chunks.Select(it => it.Trim()).Where(it => it.Length > 0).ToList();
        }

        private static IEnumerable<string> SplitBlock(string block)
        {
            block = block.Trim();
            if (block.Length <= MaxChunkLength)
            {
                yield return block;
                yield break;
            }

            var current = new StringBuilder();
            foreach (string sentence in SentenceRegex().Split(block))
            {
                if (sentence.Length > MaxChunkLength)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString().Trim();
                        current.Clear();
                    }

                    for (int index = 0; index < sentence.Length; index += MaxChunkLength)
                    {
                        yield return sentence.Substring(index, Math.Min(MaxChunkLength, sentence.Length - index));
                    }
                    continue;
                }

                if (current.Length > 0 && current.Length + sentence.Length > MaxChunkLength)
                {
                    yield return current.ToString().Trim();
                    current.Clear();
                }

                current.Append(sentence);
            }

            if (current.Length > 0)
            {
                yield return current.ToString().Trim();
            }
        }

        private static string GetOverlap(string chunk)
        {
            if (chunk.Length <= OverlapLength)
            {
                return chunk;
            }

            string overlap = chunk[^OverlapLength..];
            int newline = overlap.IndexOf('\n');
            return newline >= 0 && newline + 1 < overlap.Length ? overlap[(newline + 1)..].Trim() : overlap.Trim();
        }
    }
}
