using System.Security.Cryptography;
using System.Text;
using NpgsqlTypes;
using Sentinel.Application.Interfaces;
using Sentinel.Domain.Constants;

namespace Sentinel.Infrastructure.Services;

public sealed class StubEmbeddingService : IEmbeddingService
{
    public Task<Vector> GenerateAsync(string input, CancellationToken cancellationToken)
    {
        var seed = CreateSeed(input);
        var random = new Random(seed);
        var values = new float[EmbeddingConstants.Dimensions];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (float)random.NextDouble();
        }

        return Task.FromResult(new Vector(values));
    }

    private static int CreateSeed(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToInt32(hash, 0);
    }
}
