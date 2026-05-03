using ElBruno.LocalEmbeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VectorDBSync.EmbeddingService
{


    internal class LocalEmbeddingService : IEmbeddingService, IDisposable, IProgress<(int done, int total)>
    {
        private readonly LocalEmbeddingGenerator _generator;
        // ── Tune these for your machine ─────────────────────────────────────
        // Batch size: how many texts sent to ONNX at once.
        // 32 is safe for 32GB RAM. Raise to 64 if you have headroom.
        // Lower to 16 if you still get memory pressure.
        private const int DefaultBatchSize = 32;

        // Delay between batches (ms) — gives CPU/RAM time to breathe.
        // Set to 0 if you want max speed and don't mind high CPU.
        private const int BatchDelayMs = 50;
        public LocalEmbeddingService(string modelName = "sentence-transformers/all-MiniLM-L6-v2")
        {
            // The library handles model downloading/loading automatically.
            // all-MiniLM-L6-v2 is the default and produces 384-dimension vectors.
            var options = new ElBruno.LocalEmbeddings.Options.LocalEmbeddingsOptions
            {
                ModelName = modelName
            };

            _generator = new LocalEmbeddingGenerator(options);
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            return await GetVectors(texts, DefaultBatchSize, this);
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(
            List<string> texts,
            int batchSize = DefaultBatchSize,
            IProgress<(int done, int total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Count == 0)
                return new List<ReadOnlyMemory<float>>();

            var results = new List<ReadOnlyMemory<float>>(texts.Count);
            int total = texts.Count;
            int done = 0;

            // Slice into batches
            for (int i = 0; i < total; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Take next chunk — last batch may be smaller
                var batch = texts
                    .Skip(i)
                    .Take(batchSize)
                    .ToArray();

                var batchResults = await _generator.GenerateAsync(batch);
                results.AddRange(batchResults.Select(r => r.Vector));

                done += batch.Length;
                progress?.Report((done, total));

                // Small delay between batches — prevents CPU/RAM from maxing out
                if (BatchDelayMs > 0 && done < total)
                    await Task.Delay(BatchDelayMs, cancellationToken);
            }

            return results;
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectorsOld(List<string> texts)
        {
            // GenerateAsync handles batching for multiple strings
            var result = await _generator.GenerateAsync(texts.ToArray());

            // Result is a list of Embedding objects; we extract the Vector property
            return result.Select(r => r.Vector).ToList();
        }

        public async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            // GenerateEmbeddingAsync is optimized for a single string
            var result = await _generator.GenerateEmbeddingAsync(text);
            return result.Vector;
        }

        public void Dispose()
        {
            // Local models use ONNX Runtime resources that should be released
            _generator?.Dispose();
        }

        public void Report((int done, int total) value)
        {
            Console.WriteLine($"Progress: {value.done}/{value.total} ({(value.done * 100) / value.total}%)");
        }
    }
}

