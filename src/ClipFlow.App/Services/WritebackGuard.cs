using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ClipFlow.App.Services;

/// <summary>
/// Previne loops infinitos quando o próprio ClipFlow escreve na área de transferência
/// para colar um item fixado ou antigo. Baseado na arquitetura do EcoPaste + Clippboard.
/// </summary>
public sealed class WritebackGuard
{
    private static readonly TimeSpan SuppressTtl = TimeSpan.FromSeconds(2.5);
    private readonly object _lock = new();
    private readonly List<(string Hash, DateTime CreatedAt)> _pending = [];

    public uint OwnSequenceNumber { get; set; }

    public void Suppress(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash)) return;
        lock (_lock)
        {
            CleanupExpired();
            _pending.Add((contentHash, DateTime.UtcNow));
        }
    }

    public bool ShouldSkip(string contentHash, uint currentSequenceNumber)
    {
        if (OwnSequenceNumber != 0 && currentSequenceNumber == OwnSequenceNumber)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(contentHash)) return false;

        lock (_lock)
        {
            CleanupExpired();
            var idx = _pending.FindIndex(x => string.Equals(x.Hash, contentHash, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;
            _pending.RemoveAt(idx);
            return true;
        }
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        _pending.RemoveAll(x => now - x.CreatedAt > SuppressTtl);
    }

    public static string ComputeTextHash(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public static string ComputeBytesHash(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexStringLower(hash);
    }
}
